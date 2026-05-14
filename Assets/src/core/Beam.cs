using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using utils;

namespace core {
    public struct Beam : IEquatable<Beam> {
        public static ushort INVALID_ID = ushort.MaxValue;
        
        public int3 tailPos { get; private set; }
        public ushort id { get; internal set; }
        public readonly AxisDirection direction;
        public ushort length;
        public int3 headPos => tailPos.offset(direction, length);

        [Flags]
        private enum BeamFlags {
            BeingEmitted = 1 << 0,
            BeingConsumed = 1 << 1,
        }

        private BeamFlags flags;
        public bool beingEmitted => (flags & BeamFlags.BeingEmitted) == BeamFlags.BeingEmitted;
        public bool beingConsumed => (flags & BeamFlags.BeingConsumed) == BeamFlags.BeingConsumed;

        public readonly BeamImage image;

        public Beam(AxisDirection direction, int3 startPos, BeamImage image) {
            this.direction = direction;
            this.tailPos = startPos;
            this.image = image;
            length = 0;
            flags = 0;
            id = INVALID_ID;
        }

        public bool isValid => id != INVALID_ID;
        internal void _invalidate() => id = INVALID_ID;

        internal void _emit(SimSpace space) {
            Assert.IsFalse(beingEmitted, "Beam already being emitted");
            flags |= BeamFlags.BeingEmitted;
        }

        internal void _stopEmit() {
            Assert.IsTrue(beingEmitted, "Beam not being emitted");
            flags &= ~BeamFlags.BeingEmitted;
        }

        internal void _consume() {
            Assert.IsFalse(beingConsumed, "Beam already being consumed");
            flags |= BeamFlags.BeingConsumed;
        }

        internal void _stopConsume() {
            Assert.IsTrue(beingConsumed, "Beam not being consumed");
            flags &= ~BeamFlags.BeingConsumed;
        }

        public bool tick() {
            if (!beingEmitted) {
                tailPos += direction.int3();
            }

            if (beingEmitted) length++;
            if (beingConsumed) length--;

            return length > 0;
        }

        public bool Equals(Beam other) => id == other.id;

        public override bool Equals(object obj) {
            return obj is Beam other && Equals(other);
        }

        public override int GetHashCode() => id;
    }

    public sealed class BeamImageData {
        public static ushort INVALID_ID = ushort.MaxValue;
        
        public readonly BeamImageDataManager manager;
        public readonly ushort id;

        public readonly int2 size;

        [CanBeNull] private RenderTexture renderTexture = null;
        // //TODO: cpu side for small stuff
        // private float4[,] data;
        // private NativeArray<float4> nativeData = default;

        private BeamImageData(ushort id, int2 size) {
            this.id = id;
            this.size = size;
        }

        private void createRenderTexture() {
            if (renderTexture) return;
            renderTexture = new RenderTexture(
                size.x, size.y,
                GraphicsFormat.R32G32B32A32_SFloat,
                GraphicsFormat.None,
                0
            );
            renderTexture.enableRandomWrite = true;
            renderTexture.useMipMap = false;
            renderTexture.autoGenerateMips = false;
            renderTexture.filterMode = FilterMode.Point;
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
            if (renderTexture == null) createRenderTexture();
            return renderTexture;
        }

        private ushort refCount = 0;

        public void incRef() => refCount++;

        public void decRef() {
            refCount--;
            if (refCount == 0) {
                if (renderTexture != null) renderTexture.Release();
                manager._free(this);
            }
        }

        public sealed class BeamImageDataManager {
            private List<BeamImageData> instances = new();
            private Stack<ushort> freeSlots = new();

            public BeamImageData get(ushort id) => instances[id];

            private BeamImageData addNew(int2 size) {
                BeamImageData data;
                if (freeSlots.TryPop(out var slot)) {
                    instances[slot] = data = new BeamImageData(slot, size);
                } else {
                    data = new BeamImageData((ushort)instances.Count, size);
                    instances.Add(data);
                }

                return data;
            }

            internal void _free(BeamImageData instance) {
                instances[instance.id] = null;
                freeSlots.Push(instance.id);
            }
        }
    }

    public readonly struct BeamImage {
        private static Texture SINGLE_PIXEL_TEXTURE = null;

        [RuntimeInitializeOnLoadMethod]
        private static void INIT() {
            var texture = new Texture2D(1, 1, GraphicsFormat.R32G32B32A32_SFloat, TextureCreationFlags.None);
            texture.SetPixel(0, 0, new Color(1, 1, 1, 1));
            texture.Apply();
            SINGLE_PIXEL_TEXTURE = texture;
        }

        private readonly ushort dataId;

        public readonly float4 modulation;
        public readonly float4 bias;

        //TODO
        // public readonly Orientation orientation;
        // public readonly byte2 scale;
        // public readonly int2 offset;

        private BeamImage(ushort dataId, float4 modulation, float4 bias) {
            this.dataId = dataId;
            this.modulation = modulation;
            this.bias = bias;
        }

        public bool isSinglePixel => dataId == BeamImageData.INVALID_ID;

        public static BeamImage singlePixel(float4 color) => new(BeamImageData.INVALID_ID, color, float4.zero);

        public BeamImage modulated(float4 modulator) => new(dataId, modulation * modulator, bias * modulator);
        public BeamImage biased(float4 offset) => new(dataId, modulation, bias + offset);

        public bool isEqualConservative(BeamImage other) =>
            dataId == other.dataId && math.all(modulation == other.modulation) && math.all(bias == other.bias);

        public BeamImageData getData(BeamImageData.BeamImageDataManager manager) {
            if (isSinglePixel) return null;
            return manager.get(dataId);
        }

        public Texture getTexture(BeamImageData.BeamImageDataManager manager) {
            if (isSinglePixel) {
                return SINGLE_PIXEL_TEXTURE;
            }

            return getData(manager).getTexture();
        }

        public void _tmp_setToCS(BeamImageData.BeamImageDataManager manager, ComputeShader cs, int kernel,
            string name) {
            if (!isSinglePixel) {
                cs.SetTexture(kernel, name, getData(manager)._tmp_getRT());
            } else {
                cs.SetTexture(kernel, name, SINGLE_PIXEL_TEXTURE);
            }

            cs.SetVector(name + "Tint", modulation);
        }
    }
}