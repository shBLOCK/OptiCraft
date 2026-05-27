using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using utils;

namespace core.beam {
    public enum BeamImageOrientation : byte {
        NegXNegY,
        PosXNegY,
        NegXPosY,
        PosXPosY,
        NegYNegX,
        PosYNegX,
        NegYPosX,
        PosYPosX
    }

    public static class BeamImageOrientationExtensions {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Sign xSign(this BeamImageOrientation orientation) => (Sign)((byte)orientation & 0b1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Sign ySign(this BeamImageOrientation orientation) =>
            (Sign)(((byte)orientation & 0b10) >> 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool isXYSwapped(this BeamImageOrientation orientation) => ((byte)orientation & 0b100) != 0;

        public static (AxisDirection x, AxisDirection y)
            basis(this BeamImageOrientation orientation, Axis normal) {
            var normalBasis = normal.orthoBasis();
            if (orientation.isXYSwapped()) {
                normalBasis = (normalBasis.y, normalBasis.x);
            }

            return (
                normalBasis.x.withSign(orientation.xSign()),
                normalBasis.y.withSign(orientation.ySign())
            );
        }

        public static BeamImageOrientation fromBasis((AxisDirection x, AxisDirection y) basis) {
            var normal = (basis.x.axis(), basis.y.axis()).orthoAxis();
            var normalBasis = normal.orthoBasis();
            var (xSign, ySign) = (basis.x.sign(), basis.y.sign());
            var xySwapped = normalBasis.x != basis.x.axis();
            return (BeamImageOrientation)((byte)xSign | ((byte)ySign << 1) | (xySwapped.to01Byte() << 2));
        }

        /// ORIENTATION_REFLECT_LUT[inDir][outDir][inOrientation] -> outOrientation
        private static readonly BeamImageOrientation[] ORIENTATION_REFLECT_LUT = Enumerable.Range(0, 6 * 6 * 8)
            .Select(i => {
                var inOrientation = (BeamImageOrientation)(i % 8);
                i /= 8;
                var outDir = (AxisDirection)(i % 6);
                i /= 6;
                var inDir = (AxisDirection)(i % 6);
                if (outDir.axis() == inDir.axis()) return inOrientation;

                var inBasis = inOrientation.basis(inDir.axis());
                var rot = inDir.cross(outDir);
                var outBasis = (inBasis.x.rotate(rot), inBasis.y.rotate(rot));
                return fromBasis(outBasis);
            }).ToArray();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BeamImageOrientation reflect(
            this BeamImageOrientation orientation,
            AxisDirection inDir, AxisDirection outDir
        ) => ORIENTATION_REFLECT_LUT[(int)inDir * 6 * 8 + (int)outDir * 8 + (int)orientation];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BeamImageOrientation antiReflect(
            this BeamImageOrientation orientation,
            AxisDirection inDir, AxisDirection outDir
        ) => orientation.reflect(inDir.opposite(), outDir);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (int2 x, int2 y) int2Basis(this BeamImageOrientation orientation) {
            var (xVal, yVal) = (orientation.xSign().intValue(), orientation.ySign().intValue());
            return orientation.isXYSwapped()
                ? (new int2(0, xVal), new int2(yVal, 0))
                : (new int2(xVal, 0), new int2(0, yVal));
        }
    }
}