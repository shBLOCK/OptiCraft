using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace utils {
    public static class MiscUtils {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 size(this Texture2D texture) => new(texture.width, texture.height);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte to01Byte(this bool value) => (byte)(value ? 1 : 0);
    }
}