using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using utils;

namespace core {
    public struct Beam : IEquatable<Beam> {
        public const ushort INVALID_ID = ushort.MaxValue;

        public int3 tailPos { get; private set; }
        public ushort id { get; internal set; }
        public readonly AxisDirection direction;
        public ushort length;
        public int3 headPos => tailPos.offset(direction, length);

        [Flags]
        private enum BeamFlags : byte {
            BeingEmitted = 1 << 0,
            BeingConsumed = 1 << 1,
        }

        private BeamFlags flags;
        public bool beingEmitted => (flags & BeamFlags.BeingEmitted) == BeamFlags.BeingEmitted;
        public bool beingConsumed => (flags & BeamFlags.BeingConsumed) == BeamFlags.BeingConsumed;

        public readonly BeamImage image;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Beam(int3 startPos, AxisDirection direction, BeamImage image) {
            this.tailPos = startPos;
            this.direction = direction;
            this.image = image;
            length = 0;
            flags = 0;
            id = INVALID_ID;
        }

        public bool isValid => id != INVALID_ID;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void _emit(BeamImageData.BeamImageDataManager beamImageDataManager) {
            Assert.IsFalse(beingEmitted, "Beam already being emitted");
            flags |= BeamFlags.BeingEmitted;
            image.incRef(beamImageDataManager);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void _stopEmit() {
            Assert.IsTrue(beingEmitted, "Beam not being emitted");
            flags &= ~BeamFlags.BeingEmitted;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void _consume() {
            Assert.IsFalse(beingConsumed, "Beam already being consumed");
            flags |= BeamFlags.BeingConsumed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void _stopConsume() {
            Assert.IsTrue(beingConsumed, "Beam not being consumed");
            flags &= ~BeamFlags.BeingConsumed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void _end(BeamImageData.BeamImageDataManager beamImageDataManager) {
            id = INVALID_ID;
            image.decRef(beamImageDataManager);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void tick() {
            if (!beingEmitted) {
                tailPos += direction.int3();
            }

            if (beingEmitted) length++;
            if (beingConsumed) length--;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Beam other) => id == other.id;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) {
            return obj is Beam other && Equals(other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => id;

        // [RuntimeInitializeOnLoadMethod]
        // private static void PRINT_LAYOUT() {
        //     DebugUtils.printStructLayout<Beam>();
        //     DebugUtils.printStructLayout<BeamImage>();
        // }
    }

    public sealed class BeamImageData {
        public const ushort INVALID_ID = ushort.MaxValue;

        public readonly BeamImageDataManager manager;
        public readonly ushort id;

        public readonly uint2 size;

        [CanBeNull] private RenderTexture renderTexture = null;
        // //TODO: cpu side for small stuff
        // private float4[,] data;
        // private NativeArray<float4> nativeData = default;

        private BeamImageData(BeamImageDataManager manager, ushort id, uint2 size) {
            this.manager = manager;
            this.id = id;
            this.size = size;
        }

        private void createRenderTexture() {
            if (renderTexture) return;
            renderTexture = new RenderTexture(
                (int)size.x, (int)size.y,
                GraphicsFormat.R32G32B32A32_SFloat,
                GraphicsFormat.None,
                0
            ) {
                enableRandomWrite = true,
                useMipMap = false,
                autoGenerateMips = false,
                filterMode = FilterMode.Point
            };
            Assert.IsTrue(renderTexture.Create());
        }

        public Texture getTexture() {
            if (!renderTexture) {
                return Texture2D.redTexture; // TODO
            }

            return renderTexture;
        }

        public void blitFromTexture(Texture texture) {
            createRenderTexture();
            Graphics.Blit(texture, renderTexture);
        }

        public RenderTexture _tmp_getRT() {
            if (!renderTexture) createRenderTexture();
            return renderTexture;
        }

        private ushort refCount = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void incRef() => refCount++;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void decRef() {
            refCount--;
            if (refCount == 0) {
                manager._free(this);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void free() {
            if (renderTexture) renderTexture.Release();
        }

        public sealed class BeamImageDataManager {
            private List<BeamImageData> instances = new();
            private Stack<ushort> freeSlots = new();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public BeamImageData get(ushort id) => instances[id];

            public BeamImageData addNew(uint2 size) {
                BeamImageData data;
                if (freeSlots.TryPop(out var slot)) {
                    instances[slot] = data = new BeamImageData(this, slot, size);
                } else {
                    data = new BeamImageData(this, (ushort)instances.Count, size);
                    instances.Add(data);
                }

                return data;
            }

            internal void _free(BeamImageData instance) {
                instance.free();
                instances[instance.id] = null;
                freeSlots.Push(instance.id);
            }

            internal void reset() {
                instances.ForEach(it => {
                    if (it != null) it.free();
                });
                instances.Clear();
                freeSlots.Clear();
            }
        }
    }

    public readonly struct BeamImage {
        public static readonly BeamImage DUMMY = new BeamImage(
            BeamImageData.INVALID_ID,
            0,
            (Orientation)byte.MaxValue,
            0,
            float.NaN,
            float.NaN
        );

        private static Texture SINGLE_PIXEL_RGBA_ONES_TEXTURE = null;

        [RuntimeInitializeOnLoadMethod]
        private static void INIT() {
            var texture = new Texture2D(1, 1, GraphicsFormat.R32G32B32A32_SFloat, TextureCreationFlags.None);
            texture.SetPixel(0, 0, new Color(1, 1, 1, 1));
            texture.Apply();
            SINGLE_PIXEL_RGBA_ONES_TEXTURE = texture;
        }

        public enum Orientation : byte {
            PosXPosY,
            NegXPosY,
            PosXNegY,
            NegXNegY,
            PosYPosX,
            NegYPosX,
            PosYNegX,
            NegYNegX,
        }

        private readonly ushort dataId;

        /// 256 is represented as 0
        private readonly byte2 _size;

        public uint2 size => (uint2)((byte2)(_size - 1) + 1);
        public readonly Orientation orientation;
        public readonly byte2 offset;
        public readonly float4 modulation;
        public readonly float4 bias;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private BeamImage(
            ushort dataId,
            byte2 _size,
            Orientation orientation,
            byte2 offset,
            float4 modulation,
            float4 bias
        ) {
            this.dataId = dataId;
            this._size = _size;
            this.orientation = orientation;
            this.offset = offset;
            this.modulation = modulation;
            this.bias = bias;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BeamImage(ushort dataId,
            uint2 size,
            Orientation orientation,
            byte2 offset,
            float4 modulation,
            float4 bias
        ) : this(dataId, (byte2)size, orientation, offset, modulation, bias) { }

        public bool isSinglePixel => dataId == BeamImageData.INVALID_ID;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BeamImage singlePixel(float4 color) =>
            new(BeamImageData.INVALID_ID, 1, Orientation.PosXPosY, 0, color, float4.zero);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BeamImage modulated(float4 modulator) =>
            new(dataId, _size, orientation, offset, modulation * modulator, bias * modulator);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BeamImage biased(float4 biasValue) =>
            new(dataId, _size, orientation, offset, modulation, bias + biasValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool isEqualConservative(BeamImage other) =>
            dataId == other.dataId
            && orientation == other.orientation
            && math.all(_size == other._size)
            && math.all(offset == other.offset)
            && math.all(modulation == other.modulation)
            && math.all(bias == other.bias);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void incRef(BeamImageData.BeamImageDataManager manager) {
            if (dataId != BeamImageData.INVALID_ID) manager.get(dataId).incRef();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void decRef(BeamImageData.BeamImageDataManager manager) {
            if (dataId != BeamImageData.INVALID_ID) manager.get(dataId).decRef();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BeamImageData getData(BeamImageData.BeamImageDataManager manager) {
            if (dataId == BeamImageData.INVALID_ID) return null;
            return manager.get(dataId);
        }

        public Texture getTexture(BeamImageData.BeamImageDataManager manager) {
            //TODO: single pixel on the GPU?
            if (isSinglePixel) {
                return SINGLE_PIXEL_RGBA_ONES_TEXTURE;
            }

            return getData(manager).getTexture();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void setToShader(
            BeamImageData.BeamImageDataManager manager,
            CommandBuffer cmds,
            ComputeShader cs,
            int kernel,
            BeamImageShaderUniform uniform
        ) {
            float2x3 transform = new float2x3(
                1f, 0f, 0f,
                0f, 1f, 0f
            );
            if (!isSinglePixel) {
                cmds.SetComputeTextureParam(cs, kernel, uniform.textureId, getData(manager)._tmp_getRT());
            } else {
                cmds.SetComputeTextureParam(cs, kernel, uniform.textureId, SINGLE_PIXEL_RGBA_ONES_TEXTURE);
                // TODO: actually set SHADER_UNIFORM_TRANSFORM
            }

            cmds.SetComputeVectorParam(cs, uniform.transformPackedId, new float4(transform.c0, transform.c1));
            cmds.SetComputeVectorParam(cs, uniform.offsetId, new float4(transform.c2, 0f, 0f));
            cmds.SetComputeVectorParam(cs, uniform.modulationId, modulation);
            cmds.SetComputeVectorParam(cs, uniform.biasId, bias);
        }
    }

    public readonly struct BeamImageShaderUniform {
        internal readonly int textureId;
        internal readonly int transformPackedId;
        internal readonly int offsetId;
        internal readonly int modulationId;
        internal readonly int biasId;

        public BeamImageShaderUniform(string name) {
            textureId = Shader.PropertyToID(name);
            transformPackedId = Shader.PropertyToID(name + "TransformPacked");
            offsetId = Shader.PropertyToID(name + "Offset");
            modulationId = Shader.PropertyToID(name + "Modulation");
            biasId = Shader.PropertyToID(name + "Bias");
        }
    }
}