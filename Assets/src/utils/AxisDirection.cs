using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Unity.Mathematics;

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
        public static (Axis, Axis) orthoAxes(this Axis axis) => axis switch {
            Axis.X => (Axis.Y, Axis.Z),
            Axis.Y => (Axis.X, Axis.Z),
            Axis.Z => (Axis.X, Axis.Y),
            _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, null)
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Axis orthoAxes(this (Axis, Axis) axes) => axes switch {
            (Axis.X, Axis.Y) => Axis.Z,
            (Axis.Y, Axis.X) => Axis.Z,
            (Axis.X, Axis.Z) => Axis.Y,
            (Axis.Z, Axis.X) => Axis.Y,
            (Axis.Y, Axis.Z) => Axis.X,
            (Axis.Z, Axis.Y) => Axis.X,
            _ => throw new ArgumentOutOfRangeException(nameof(axes), axes, null)
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Axis rotate(this Axis self, Axis axis) {
            if (self == axis) return self;
            return orthoAxes((self, axis));
        }
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AxisDirection opposite(this AxisDirection direction) =>
            (AxisDirection)(((byte)direction & 0b110) | (~(byte)direction & 0b1));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Axis axis(this AxisDirection direction) => (Axis)((byte)direction >> 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 offset(this int3 pos, AxisDirection direction, int value = 1) => pos + direction.int3(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Sign sign(this AxisDirection direction) => (Sign)((byte)direction & 0b1);

        //@formatter:off
        private static AxisDirection[] ROTATION_LUT = new AxisDirection[6 * 6] {
            AxisDirection.NegX, AxisDirection.PosX, AxisDirection.NegZ, AxisDirection.PosZ, AxisDirection.PosY, AxisDirection.NegY,
            AxisDirection.NegX, AxisDirection.PosX, AxisDirection.PosZ, AxisDirection.NegZ, AxisDirection.NegY, AxisDirection.PosY,
            AxisDirection.PosZ, AxisDirection.NegZ, AxisDirection.NegY, AxisDirection.PosY, AxisDirection.NegX, AxisDirection.PosX,
            AxisDirection.NegZ, AxisDirection.PosZ, AxisDirection.NegY, AxisDirection.PosY, AxisDirection.PosX, AxisDirection.NegX,
            AxisDirection.NegY, AxisDirection.PosY, AxisDirection.PosX, AxisDirection.NegX, AxisDirection.NegZ, AxisDirection.PosZ,
            AxisDirection.PosY, AxisDirection.NegY, AxisDirection.NegX, AxisDirection.PosX, AxisDirection.NegZ, AxisDirection.PosZ,
        };
        //@formatter:on

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AxisDirection rotate(this AxisDirection self, AxisDirection axis) =>
            ROTATION_LUT[(byte)axis * 6 + (byte)self];
    }
}