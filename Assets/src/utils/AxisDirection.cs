using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace utils {
    public enum Sign : byte {
        Neg,
        Pos
    }

    public static class SignExtensions {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Sign opposite(this Sign sign) => (Sign)((~(byte)sign) & 0b1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float floatValue(this Sign sign) => sign == Sign.Neg ? -1f : 1f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int intValue(this Sign sign) => sign == Sign.Neg ? -1 : 1;
    }

    public enum Axis : byte {
        X,
        Y,
        Z
    }

    public static class AxisExtensions {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AxisDirection withSign(this Axis axis, Sign sign) =>
            (AxisDirection)(((byte)axis << 1) | (byte)sign);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 float3(this Axis axis, float length = 1f) => axis switch {
            Axis.X => new float3(length, 0, 0),
            Axis.Y => new float3(0, length, 0),
            Axis.Z => new float3(0, 0, length),
            _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, null)
        };
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 int3(this Axis axis, int length = 1) => axis switch {
            Axis.X => new int3(length, 0, 0),
            Axis.Y => new int3(0, length, 0),
            Axis.Z => new int3(0, 0, length),
            _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, null)
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (Axis x, Axis y) orthoBasis(this Axis axis) => axis switch {
            Axis.X => (Axis.Y, Axis.Z),
            Axis.Y => (Axis.X, Axis.Z),
            Axis.Z => (Axis.X, Axis.Y),
            _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, null)
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Axis orthoAxis(this (Axis, Axis) axes) => (Axis)(3 - (byte)axes.Item1 - (byte)axes.Item2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Axis rotate(this Axis self, Axis axis) {
            if (self == axis) return self;
            return orthoAxis((self, axis));
        }

        private static readonly quaternion[] MODEL_ROTATION_LUT = Enumerable.Range(0, 3)
            .Select(i => (quaternion)Quaternion.FromToRotation(Vector3.forward, ((Axis)i).float3()))
            .ToArray();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion modelRotation(this Axis axis) => MODEL_ROTATION_LUT[(byte)axis];
    }

    public enum AxisDirection : byte {
        NegX,
        PosX,
        NegY,
        PosY,
        NegZ,
        PosZ
    }

    public static class AxisDirectionExtensions {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 float3(this AxisDirection direction, float length = 1f) => direction switch {
            AxisDirection.NegX => new float3(-length, 0, 0),
            AxisDirection.PosX => new float3(length, 0, 0),
            AxisDirection.NegY => new float3(0, -length, 0),
            AxisDirection.PosY => new float3(0, length, 0),
            AxisDirection.NegZ => new float3(0, 0, -length),
            AxisDirection.PosZ => new float3(0, 0, length),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 int3(this AxisDirection direction, int length = 1) => direction switch {
            AxisDirection.NegX => new int3(-length, 0, 0),
            AxisDirection.PosX => new int3(length, 0, 0),
            AxisDirection.NegY => new int3(0, -length, 0),
            AxisDirection.PosY => new int3(0, length, 0),
            AxisDirection.NegZ => new int3(0, 0, -length),
            AxisDirection.PosZ => new int3(0, 0, length),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };

        public static AxisDirection fromNormInt3(int3 vec) => vec switch {
            { x: -1, y: 0, z: 0 } => AxisDirection.NegX,
            { x: 1, y: 0, z: 0 } => AxisDirection.PosX,
            { x: 0, y: -1, z: 0 } => AxisDirection.NegY,
            { x: 0, y: 1, z: 0 } => AxisDirection.PosY,
            { x: 0, y: 0, z: -1 } => AxisDirection.NegZ,
            { x: 0, y: 0, z: 1 } => AxisDirection.PosZ,
            _ => throw new ArgumentOutOfRangeException(nameof(vec), vec, null)
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AxisDirection opposite(this AxisDirection direction) =>
            (AxisDirection)(((byte)direction & 0b110) | (~(byte)direction & 0b1));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Axis axis(this AxisDirection direction) => (Axis)((byte)direction >> 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 offset(this int3 pos, AxisDirection direction, int value = 1) => pos + direction.int3(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Sign sign(this AxisDirection direction) => (Sign)((byte)direction & 0b1);

        private static readonly AxisDirection[] ROTATION_LUT = Enumerable.Range(0, 6 * 6)
            .Select(i => {
                var axis = (AxisDirection)(i / 6 % 6);
                var vec = (AxisDirection)(i % 6);
                var rotated = quaternion.AxisAngle(axis.float3(), math.PIHALF).mul(vec.float3());
                return fromNormInt3(rotated.rint());
            }).ToArray();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AxisDirection rotate(this AxisDirection self, AxisDirection axis) =>
            ROTATION_LUT[(byte)axis * 6 + (byte)self];

        private static readonly AxisDirection[] CROSS_LUT = Enumerable.Range(0, 6 * 6)
            .Select(i => {
                var a = (AxisDirection)(i / 6 % 6);
                var b = (AxisDirection)(i % 6);
                if (a.axis() == b.axis()) return (AxisDirection)byte.MaxValue;
                var vec = mathx.cross(a.int3(), b.int3());
                return fromNormInt3(vec);
            }).ToArray();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AxisDirection cross(this AxisDirection a, AxisDirection b) => CROSS_LUT[(byte)a * 6 + (byte)b];

        private static readonly quaternion[] MODEL_ROTATION_LUT = Enumerable.Range(0, 6)
            .Select(i => (quaternion)Quaternion.FromToRotation(Vector3.forward, ((AxisDirection)i).float3()))
            .ToArray();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion modelRotation(this AxisDirection direction) => MODEL_ROTATION_LUT[(byte)direction];
    }
}