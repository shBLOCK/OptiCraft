using System;
using System.Runtime.CompilerServices;

namespace utils {
    // ReSharper disable once ShiftExpressionZeroLeftOperand
    public enum MirrorDirection : byte {
        // 0b00_dirB_dirA
        // dirA <= dirB
        NegXNegX = (AxisDirection.NegX << 3) | AxisDirection.NegX,
        NegXNegY = (AxisDirection.NegY << 3) | AxisDirection.NegX,
        NegXPosY = (AxisDirection.PosY << 3) | AxisDirection.NegX,
        NegXNegZ = (AxisDirection.NegZ << 3) | AxisDirection.NegX,
        NegXPosZ = (AxisDirection.PosZ << 3) | AxisDirection.NegX,
        PosXPosX = (AxisDirection.PosX << 3) | AxisDirection.PosX,
        PosXNegY = (AxisDirection.NegY << 3) | AxisDirection.PosX,
        PosXPosY = (AxisDirection.PosY << 3) | AxisDirection.PosX,
        PosXNegZ = (AxisDirection.NegZ << 3) | AxisDirection.PosX,
        PosXPosZ = (AxisDirection.PosZ << 3) | AxisDirection.PosX,
        NegYNegY = (AxisDirection.NegY << 3) | AxisDirection.NegY,
        NegYNegZ = (AxisDirection.NegZ << 3) | AxisDirection.NegY,
        NegYPosZ = (AxisDirection.PosZ << 3) | AxisDirection.NegY,
        PosYPosY = (AxisDirection.PosY << 3) | AxisDirection.PosY,
        PosYNegZ = (AxisDirection.NegZ << 3) | AxisDirection.PosY,
        PosYPosZ = (AxisDirection.PosZ << 3) | AxisDirection.PosY,
        NegZNegZ = (AxisDirection.NegZ << 3) | AxisDirection.NegZ,
        PosZPosZ = (AxisDirection.PosZ << 3) | AxisDirection.PosZ,
    }

    public static class MirrorDirectionExtensions {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AxisDirection dirA(this MirrorDirection md) => (AxisDirection)((byte)md & 0b111);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AxisDirection dirB(this MirrorDirection md) => (AxisDirection)((byte)md >> 3);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool reflect(this MirrorDirection md, AxisDirection dir, out AxisDirection reflectDir,
            out bool isFrontSide) {
            if (dir == md.dirA()) {
                reflectDir = md.dirB();
                isFrontSide = true;
                return true;
            } else if (dir == md.dirB()) {
                reflectDir = md.dirA();
                isFrontSide = true;
                return true;
            } else if (dir == md.dirA().opposite()) {
                reflectDir = md.dirB().opposite();
                isFrontSide = false;
                return true;
            } else if (dir == md.dirB().opposite()) {
                reflectDir = md.dirA().opposite();
                isFrontSide = false;
                return true;
            }

            reflectDir = default;
            isFrontSide = default;
            return false;
        }

        public static (AxisDirection, AxisDirection) getDirOnAxisAndOtherDir(this MirrorDirection md, Axis axis) {
            var dirA = md.dirA();
            var dirB = md.dirB();
            if (dirA.axis() == axis) return (dirA, dirB);
            if (dirB.axis() == axis) return (dirB, dirA);
            throw new InvalidOperationException($"No matching direction in {md} on {axis}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MirrorDirection fromDirections(AxisDirection dir1, AxisDirection dir2) {
            byte dir1byte = (byte)dir1;
            byte dir2byte = (byte)dir2;
            if (dir1byte <= dir2byte) {
                return (MirrorDirection)((dir2byte << 3) | dir1byte);
            } else {
                return (MirrorDirection)((dir1byte << 3) | dir2byte);
            }
        }

        public static MirrorDirection rotateStep(this MirrorDirection md, AxisDirection axis) {
            var dir1 = md.dirA();
            var dir2 = md.dirB();
            if (dir1.axis() == axis.axis() || dir2.axis() == axis.axis()) {
                dir1 = dir1.rotate(axis);
                dir2 = dir2.rotate(axis);
            } else {
                if (dir1 == dir2) {
                    dir2 = dir2.rotate(axis);
                } else {
                    if (dir1.rotate(axis) == dir2) {
                        dir1 = dir2;
                    } else {
                        dir2 = dir1;
                    }
                }
            }

            return fromDirections(dir1, dir2);
        }
    }
}