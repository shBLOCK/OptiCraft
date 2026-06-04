using System;
using System.Runtime.CompilerServices;

namespace utils {
    public struct ByteEnumMap<K, V> where K : Enum {
        internal readonly V[] arr;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ByteEnumMap(int entries, V initialValue) {
            arr = CollectionUtils.newFilledArray(entries, initialValue);
        }

        public ref V this[K key] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref arr[Unsafe.As<K, byte>(ref key)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void fill(V value) => Array.Fill(arr, value);
    }

    public static class ByteEnumMapExtensions {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void replaceAll<K, V>(this ByteEnumMap<K, V> self, V oldValue, V newValue)
            where K : Enum where V : IEquatable<V> {
            self.arr.replaceAll(oldValue, newValue);
        }
    }
}