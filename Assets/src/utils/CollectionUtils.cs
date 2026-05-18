using System;
using System.Runtime.CompilerServices;

namespace utils {
    public static class CollectionUtils {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] newFilledArray<T>(int size, T value) {
            var arr = new T[size];
            Array.Fill(arr, value);
            return arr;
        }
    }
}