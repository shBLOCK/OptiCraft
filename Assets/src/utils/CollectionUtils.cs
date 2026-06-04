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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void replaceThis<T>(this ref T self, in T oldValue, in T newValue) where T : struct, IEquatable<T> {
            if (self.Equals(oldValue)) {
                self = newValue;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void replaceAll<T>(this T[] array, T oldValue, T newValue) where T : IEquatable<T> {
            for (int i = 0; i < array.Length; i++) {
                ref var element = ref array[i];
                if (element.Equals(oldValue)) {
                    element = newValue;
                }
            }
        }
    }
}