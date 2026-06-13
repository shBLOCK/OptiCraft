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
            (Orientation2D)byte.MaxValue,
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

        /// The 2D basis in UV space used when sampling the image.
        /// (column 0 is the +u axis and column 1 is the +v axis)
        /// In 3D, this 2D basis is in the UV space represented by Axis.orthoBasis() of the axis of the beam.
        public readonly Orientation2D orientation;

        public readonly byte2 offset;
        public readonly float4 modulation;
        public readonly float4 bias;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private BeamImage(
            ushort dataId,
            byte2 _size,
            Orientation2D orientation,
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
            Orientation2D orientation,
            byte2 offset,
            float4 modulation,
            float4 bias
        ) : this(dataId, (byte2)size, orientation, offset, modulation, bias) { }

        public bool isSinglePixel => dataId == BeamImageData.INVALID_ID;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BeamImage singlePixel(float4 color) =>
            new(BeamImageData.INVALID_ID, 1, Orientation2D.PosXPosY, 0, color, float4.zero);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BeamImage modulated(float4 modulator) =>
            new(dataId, _size, orientation, offset, modulation * modulator, bias * modulator);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BeamImage biased(float4 biasValue) =>
            new(dataId, _size, orientation, offset, modulation, bias + biasValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BeamImage withOrientation(Orientation2D newOrientation) =>
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
            Orientation2D basis = Orientation2D.PosXPosY
        ) {
            var uvBasis = basis.mul(orientation.inverse()).inverse();
            var transform = float2x3.zero;
            if (!isSinglePixel) {
                var data = getData(manager);
                cmds.SetComputeTextureParam(cs, kernel, uniform.texture, data._tmp_getRT());
                var uvSize = new float2(size) / data.size;
                if (!uvBasis.isXYSwapped()) {
                    transform.c0.x = uvSize.x * uvBasis.xSign().floatValue();
                    transform.c1.y = uvSize.y * uvBasis.ySign().floatValue();
                } else {
                    transform.c0.y = uvSize.x * uvBasis.xSign().floatValue();
                    transform.c1.x = uvSize.y * uvBasis.ySign().floatValue();
                }

                transform.c2 = new float2(
                    uvBasis.xSign() == Sign.Neg ? 1f : 0f,
                    uvBasis.ySign() == Sign.Neg ? 1f : 0f
                ) * (uvBasis.isXYSwapped() ? uvSize.yx : uvSize);
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