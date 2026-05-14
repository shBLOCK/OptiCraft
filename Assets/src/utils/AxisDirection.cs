using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Mathematics;

namespace utils {
    public enum Axis : byte {
        X,
        Y,
        Z
    }

    public static class AxisExtensions {
        public static AxisDirection negDirection(this Axis axis) => (AxisDirection)(((byte)axis << 1) | 0);
        public static AxisDirection posDirection(this Axis axis) => (AxisDirection)(((byte)axis << 1) | 1);

        public static float3 float3(this Axis axis, float length = 1f) => axis switch {
            Axis.X => new float3(length, 0, 0),
            Axis.Y => new float3(0, length, 0),
            Axis.Z => new float3(0, 0, length),
            _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, null)
        };

        public static (Axis, Axis) orthoAxes(this Axis axis) => axis switch {
            Axis.X => (Axis.Y, Axis.Z),
            Axis.Y => (Axis.X, Axis.Z),
            Axis.Z => (Axis.X, Axis.Y),
            _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, null)
        };
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
        public static float3 float3(this AxisDirection direction, float length = 1f) => direction switch {
            AxisDirection.NegX => new float3(-length, 0, 0),
            AxisDirection.PosX => new float3(length, 0, 0),
            AxisDirection.NegY => new float3(0, -length, 0),
            AxisDirection.PosY => new float3(0, length, 0),
            AxisDirection.NegZ => new float3(0, 0, -length),
            AxisDirection.PosZ => new float3(0, 0, length),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };

        public static int3 int3(this AxisDirection direction, int length = 1) => direction switch {
            AxisDirection.NegX => new int3(-length, 0, 0),
            AxisDirection.PosX => new int3(length, 0, 0),
            AxisDirection.NegY => new int3(0, -length, 0),
            AxisDirection.PosY => new int3(0, length, 0),
            AxisDirection.NegZ => new int3(0, 0, -length),
            AxisDirection.PosZ => new int3(0, 0, length),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };

        public static AxisDirection opposite(this AxisDirection direction) =>
            (AxisDirection)(((byte)direction & 0b110) | (~(byte)direction & 0b1));

        public static Axis axis(this AxisDirection direction) => (Axis)((byte)direction >> 1);

        public static int3 offset(this int3 pos, AxisDirection direction, int value = 1) => pos + direction.int3(value);

        public static bool isNeg(this AxisDirection direction) => ((byte)direction & 0b1) == 0;
        public static bool isPos(this AxisDirection direction) => ((byte)direction & 0b1) != 0;

        //@formatter:off
        private static AxisDirection[,] ROTATION_LUT = new AxisDirection[6, 6] {
            {AxisDirection.NegX, AxisDirection.PosX, AxisDirection.PosZ, AxisDirection.NegZ, AxisDirection.NegY, AxisDirection.PosY},
            {AxisDirection.NegX, AxisDirection.PosX, AxisDirection.NegZ, AxisDirection.PosZ, AxisDirection.PosY, AxisDirection.NegY},
            {AxisDirection.NegZ, AxisDirection.PosZ, AxisDirection.NegY, AxisDirection.PosY, AxisDirection.PosX, AxisDirection.NegX},
            {AxisDirection.PosZ, AxisDirection.NegZ, AxisDirection.NegY, AxisDirection.PosY, AxisDirection.NegX, AxisDirection.PosX},
            {AxisDirection.PosY, AxisDirection.NegY, AxisDirection.NegX, AxisDirection.PosX, AxisDirection.NegZ, AxisDirection.PosZ},
            {AxisDirection.NegY, AxisDirection.PosY, AxisDirection.PosX, AxisDirection.NegX, AxisDirection.NegZ, AxisDirection.PosZ},
        };
        //@formatter:on

        public static AxisDirection ccw(this AxisDirection self, AxisDirection axis) =>
            ROTATION_LUT[(byte)axis, (byte)self];

        public static AxisDirection cw(this AxisDirection self, AxisDirection axis) => self.ccw(axis.opposite());
    }

    public class AxisDirectionMap<T> {
        private T[] map = new T[6];

        public AxisDirectionMap(T initialValue) {
            fill(initialValue);
        }

        public T this[AxisDirection key] {
            get => map[(byte)key];
            set => map[(byte)key] = value;
        }

        public void fill(T value) {
            for (int i = 0; i < 6; i++) {
                map[i] = value;
            }
        }
    }
}