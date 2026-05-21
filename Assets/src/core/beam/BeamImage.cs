using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using utils;

namespace core.beam {
    public readonly struct BeamImage {
        public static readonly BeamImage DUMMY = new BeamImage(
            BeamImageData.INVALID_ID,
            0,
            (BeamImageOrientation)byte.MaxValue,
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

        private readonly ushort dataId;

        /// 256 is represented as 0
        private readonly byte2 _size;

        public uint2 size => (uint2)((byte2)(_size - 1) + 1);
        public readonly BeamImageOrientation orientation;
        public readonly byte2 offset;
        public readonly float4 modulation;
        public readonly float4 bias;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private BeamImage(
            ushort dataId,
            byte2 _size,
            BeamImageOrientation orientation,
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
            BeamImageOrientation orientation,
            byte2 offset,
            float4 modulation,
            float4 bias
        ) : this(dataId, (byte2)size, orientation, offset, modulation, bias) { }

        public bool isSinglePixel => dataId == BeamImageData.INVALID_ID;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BeamImage singlePixel(float4 color) =>
            new(BeamImageData.INVALID_ID, 1, BeamImageOrientation.PosXPosY, 0, color, float4.zero);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BeamImage modulated(float4 modulator) =>
            new(dataId, _size, orientation, offset, modulation * modulator, bias * modulator);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BeamImage biased(float4 biasValue) =>
            new(dataId, _size, orientation, offset, modulation, bias + biasValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BeamImage withOrientation(BeamImageOrientation newOrientation) =>
            new(dataId, _size, newOrientation, offset, modulation, bias);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool isEqualConservative(BeamImage other) =>
            dataId == other.dataId
            && orientation == other.orientation
            && math.all(_size == other._size)
            && math.all(offset == other.offset)
            && math.all(modulation == other.modulation)
            && math.all(bias == other.bias);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void incRef(BeamImageData.Manager manager) {
            if (dataId != BeamImageData.INVALID_ID) manager.get(dataId).incRef();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void decRef(BeamImageData.Manager manager) {
            if (dataId != BeamImageData.INVALID_ID) manager.get(dataId).decRef();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BeamImageData getData(BeamImageData.Manager manager) {
            if (dataId == BeamImageData.INVALID_ID) return null;
            return manager.get(dataId);
        }

        public Texture getTexture(BeamImageData.Manager manager) {
            //TODO: single pixel on the GPU?
            if (isSinglePixel) {
                return SINGLE_PIXEL_RGBA_ONES_TEXTURE;
            }

            return getData(manager).getTexture();
        }

        public void setToShader(
            BeamImageData.Manager manager,
            CommandBuffer cmds,
            ComputeShader cs,
            int kernel,
            BeamImageShaderUniform uniform,
            BeamImageOrientation orientation
        ) {
            var transform = float2x3.zero;
            if (!isSinglePixel) {
                var data = getData(manager);
                cmds.SetComputeTextureParam(cs, kernel, uniform.texture, data._tmp_getRT());
                var uvSize = new float2(size) / data.size;
                if (!orientation.isXYSwapped()) {
                    transform.c0.x = uvSize.x * orientation.xSign().floatValue();
                    transform.c1.y = uvSize.y * orientation.ySign().floatValue();
                } else {
                    transform.c0.y = uvSize.x * orientation.xSign().floatValue();
                    transform.c1.x = uvSize.y * orientation.ySign().floatValue();
                }

                transform.c2 = new float2(
                    orientation.xSign() == Sign.Neg ? 1f : 0f,
                    orientation.ySign() == Sign.Neg ? 1f : 0f
                ) * uvSize;
                if (orientation.isXYSwapped()) transform.c2 = transform.c2.yx;
                transform.c2 += new float2(offset.x, offset.y);
            } else {
                cmds.SetComputeTextureParam(cs, kernel, uniform.texture, SINGLE_PIXEL_RGBA_ONES_TEXTURE);
            }

            //TODO: tmp to disable orientation stuff
            transform = new float2x3(
                1f, 0f, 0f,
                0f, 1f, 0f
            );

            cmds.SetComputeVectorParam(cs, uniform.transformPacked, new float4(transform.c0, transform.c1));
            cmds.SetComputeVectorParam(cs, uniform.offset, new float4(transform.c2, 0f, 0f));

            cmds.SetComputeVectorParam(cs, uniform.modulation, modulation);
            cmds.SetComputeVectorParam(cs, uniform.bias, bias);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void setToShader(
            BeamImageData.Manager manager,
            CommandBuffer cmds,
            ComputeShader cs,
            int kernel,
            BeamImageShaderUniform uniform
        ) => setToShader(manager, cmds, cs, kernel, uniform, orientation);
    }

    public readonly struct BeamImageShaderUniform {
        internal readonly int texture;
        internal readonly int transformPacked;
        internal readonly int offset;
        internal readonly int modulation;
        internal readonly int bias;

        public BeamImageShaderUniform(string name) {
            texture = Shader.PropertyToID(name);
            transformPacked = Shader.PropertyToID(name + "TransformPacked");
            offset = Shader.PropertyToID(name + "Offset");
            modulation = Shader.PropertyToID(name + "Modulation");
            bias = Shader.PropertyToID(name + "Bias");
        }
    }
}