using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace utils {
    public static class MiscUtils {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 size(this Texture2D texture) => new(texture.width, texture.height);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte to01Byte(this bool value) => (byte)(value ? 1 : 0);

        public static byte[] encodeToEXRWithBestCompression(
            this Texture2D tex,
            Texture2D.EXRFlags flags = Texture2D.EXRFlags.None
        ) {
            int bestSize = int.MaxValue;
            byte[] best = null;
            foreach (var compression in new[] { Texture2D.EXRFlags.CompressZIP, Texture2D.EXRFlags.CompressRLE, Texture2D.EXRFlags.CompressPIZ }) {
                var data = tex.EncodeToEXR(flags | compression);
                if (data.Length < bestSize) {
                    bestSize = data.Length;
                    best = data;
                }
            }

            return best;
        }
    }
}