from enum import Enum
from spatium import *
import itertools
from math import *


class Dir(Enum):
    NegX = 0, Vec3i(-1, 0, 0)
    PosX = 1, Vec3i(1, 0, 0)
    NegY = 2, Vec3i(0, -1, 0)
    PosY = 3, Vec3i(0, 1, 0)
    NegZ = 4, Vec3i(0, 0, -1)
    PosZ = 5, Vec3i(0, 0, 1)

    def __init__(self, index: int, vec: Vec3i):
        self.index = index
        self.vec = vec


def gen_MirrorDirection():
    mirror_dirs = []

    for a, b in itertools.combinations_with_replacement(Dir, 2):
        if a.vec != -b.vec:
            mirror_dirs.append((a, b))

    print("protected enum MirrorDirection : byte {")
    for a, b in mirror_dirs:
        print(f"    {a.name}{b.name} = (AxisDirection.{b.name} << 3) | AxisDirection.{a.name},")
    print("}")


def gen_AxisDirection_rotation():
    vec_to_dir = {dir.vec: dir for dir in Dir}
    for axis in Dir:
        # print("{", end="")
        for dir in Dir:
            rotated = vec_to_dir[Vec3i(Transform3D.rotating(Vec3(axis.vec), -pi / 2) * Vec3(dir.vec))]
            print(f"AxisDirection.{rotated.name},", end="")
        # print("},")
        print()

gen_AxisDirection_rotation()