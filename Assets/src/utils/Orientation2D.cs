using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace utils;

public enum Orientation2D : byte {
    NegXNegY = 0,
    PosXNegY = 1,
    NegXPosY = 2,
    PosXPosY = 3,
    NegYNegX = 4,
    PosYNegX = 5,
    NegYPosX = 6,
    PosYPosX = 7
}

public static class Orientation2DExtensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Sign xSign(this Orientation2D orientation) => (Sign)((byte)orientation & 0b1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Sign ySign(this Orientation2D orientation) =>
        (Sign)(((byte)orientation & 0b10) >> 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool isXYSwapped(this Orientation2D orientation) => ((byte)orientation & 0b100) != 0;

    public static (AxisDirection x, AxisDirection y) basis(this Orientation2D orientation, Axis normal) {
        var normalBasis = normal.orthoBasis();
        if (orientation.isXYSwapped()) {
            normalBasis = (normalBasis.y, normalBasis.x);
        }

        return (
            normalBasis.x.withSign(orientation.xSign()),
            normalBasis.y.withSign(orientation.ySign())
        );
    }

    public static Orientation2D fromBasis((AxisDirection x, AxisDirection y) basis) {
        var normal = (basis.x.axis(), basis.y.axis()).orthoAxis();
        var normalBasis = normal.orthoBasis();
        var (xSign, ySign) = (basis.x.sign(), basis.y.sign());
        var xySwapped = normalBasis.x != basis.x.axis();
        return (Orientation2D)((byte)xSign | ((byte)ySign << 1) | (xySwapped.to01Byte() << 2));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int2x2 int2x2(this Orientation2D orientation) {
        var (xVal, yVal) = (orientation.xSign().intValue(), orientation.ySign().intValue());
        return orientation.isXYSwapped()
            ? new int2x2(new int2(0, xVal), new int2(yVal, 0))
            : new int2x2(new int2(xVal, 0), new int2(0, yVal));
    }

    private static readonly Orientation2D[] ORIENTATION_MUL_LUT = Enumerable.Range(0, 8 * 8)
        .Select(i => {
            var b = (Orientation2D)(i & 0b111);
            var a = (Orientation2D)(i >> 3);
            var mat = math.mul(a.int2x2(), b.int2x2());
            var xySwapped = mat.c0.x == 0;
            var (xSign, ySign) = xySwapped
                ? (mat.c0.y.signEnum(), mat.c1.x.signEnum())
                : (mat.c0.x.signEnum(), mat.c1.y.signEnum());
            return (Orientation2D)((byte)xSign | ((byte)ySign << 1) | (xySwapped.to01Byte() << 2));
        }).ToArray();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Orientation2D mul(this Orientation2D a, Orientation2D b) =>
        ORIENTATION_MUL_LUT[((int)a << 3) | (int)b];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Orientation2D inverse(this Orientation2D orientation) =>
        orientation.isXYSwapped()
            ? (Orientation2D)(
                ((byte)orientation & 0b100)
                | (((byte)orientation & 0b001) << 1)
                | (((byte)orientation & 0b010) >> 1)
            )
            : orientation;

    /// ORIENTATION_REFLECT_LUT[inDir][outDir][inOrientation] -> outOrientation
    private static readonly Orientation2D[] ORIENTATION_REFLECT_LUT = Enumerable.Range(0, 6 * 6 * 8)
        .Select(i => {
            var inOrientation = (Orientation2D)(i % 8);
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
    public static Orientation2D reflect(
        this Orientation2D orientation,
        AxisDirection inDir, AxisDirection outDir
    ) => ORIENTATION_REFLECT_LUT[(int)inDir * 6 * 8 + (int)outDir * 8 + (int)orientation];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Orientation2D antiReflect(
        this Orientation2D orientation,
        AxisDirection inDir, AxisDirection outDir
    ) => orientation.reflect(inDir.opposite(), outDir);
}