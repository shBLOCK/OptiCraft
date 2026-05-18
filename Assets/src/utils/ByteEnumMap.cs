using System;
using System.Runtime.CompilerServices;

namespace utils {
    public struct ByteEnumMap<K, V> where K : Enum {
        private readonly V[] arr;

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
}