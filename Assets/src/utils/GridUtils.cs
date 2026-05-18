using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace utils {
    public static class GridUtils {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool isGridCenter(int3 pos) => pos.x % 2 == 0 && pos.y % 2 == 0 && pos.z % 2 == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool isGridEdge(int3 pos) => !isGridCenter(pos);
        //public static bool isValidGridPos(int3 pos) => TODO;
    }
}