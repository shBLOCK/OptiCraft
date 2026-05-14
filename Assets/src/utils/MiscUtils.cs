using Unity.Mathematics;
using UnityEngine;

namespace utils {
    public static class MiscUtils {
        public static int2 size(this Texture2D texture) => new(texture.width, texture.height);
    }
}