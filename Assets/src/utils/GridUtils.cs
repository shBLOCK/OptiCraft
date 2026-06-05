using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace utils {
    public static class GridUtils {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool isGridCenter(int3 pos) => (pos % 2 == 0).all();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool isGridEdge(int3 pos) => (pos % 2 == 0).asint().csum() == 2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool isValidGridPos(int3 pos) => (pos % 2 == 0).asint().csum() >= 2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool isGridEdgeAlongAxis(int3 pos, Axis axis) => isGridEdge(pos) && pos[(byte)axis] % 2 != 0;
    }
}