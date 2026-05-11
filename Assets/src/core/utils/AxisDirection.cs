using System;
using Unity.Mathematics;

namespace core {
    public enum Axis : byte {
        X,
        Y,
        Z
    }

    public static class AxisExtensions {
        public static AxisDirection negDirection(this Axis axis) => (AxisDirection)(((byte)axis << 1) | 0);
        public static AxisDirection posDirection(this Axis axis) => (AxisDirection)(((byte)axis << 1) | 1);
        
        public static float3 float3(this Axis axis) => axis switch {
            Axis.X => new float3(1, 0, 0),
            Axis.Y => new float3(0, 1, 0),
            Axis.Z => new float3(0, 0, 1),
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

        public static Axis axis(this AxisDirection direction) => (Axis)((byte)direction / 2);
        
        public static int3 offset(this int3 pos, AxisDirection direction, int value = 1) => pos + direction.int3(value);

        public static bool isNeg(this AxisDirection direction) => ((byte)direction & 0b1) == 0;
        public static bool isPos(this AxisDirection direction) => ((byte)direction & 0b1) != 0;
    }
}