using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace utils {
    public static class DebugUtils {
        public static void printStructLayout<T>() where T : struct {
            var type = typeof(T);
            Debug.Log($"===== Struct: {type.Name} =====");
            Debug.Log($"Align: {UnsafeUtility.AlignOf<T>()}");
            Debug.Log($"Size: {UnsafeUtility.SizeOf<T>()}");
            var fields = new List<(FieldInfo, int)>();
            foreach (var field in type.GetFields(~BindingFlags.Static)) {
                fields.Add((field, UnsafeUtility.GetFieldOffset(field)));
            }

            foreach (var (field, offset) in fields.OrderBy(tuple => tuple.Item2)) {
                Debug.Log($"Field \"{field.Name}\" at offset {offset}");
            }
        }
    }
}