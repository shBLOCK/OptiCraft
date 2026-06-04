using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using Unity.Mathematics;
using UnityEngine;

namespace utils {
    public static class MathUtils {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ceilDiv(this uint self, uint other) => (self + other - 1) / other;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint2 ceilDiv(this uint2 self, uint2 value) =>
            new(self.x.ceilDiv(value.x), self.y.ceilDiv(value.y));

        public static JsonArray toJsonArray(this int3 value) => new() { value.x, value.y, value.z };

        public static int3 toInt3(this JsonArray data) =>
            new(data[0].GetValue<int>(), data[1].GetValue<int>(), data[2].GetValue<int>());

        public static JsonArray toJsonArray(this float4 value) => new() { value.x, value.y, value.z, value.w };

        public static float4 toFloat4(this JsonArray data) =>
            new(data[0].GetValue<float>(), data[1].GetValue<float>(),
                data[2].GetValue<float>(), data[3].GetValue<float>());

        public static bool intersectRay(this Bounds bounds, Ray ray, out AxisDirection normal) {
            if (bounds.IntersectRay(ray, out float distance)) {
                float3 pos = ray.GetPoint(distance);
                if (Mathf.Approximately(pos.x, bounds.min.x)) {
                    normal = AxisDirection.NegX;
                } else if (Mathf.Approximately(pos.x, bounds.max.x)) {
                    normal = AxisDirection.PosX;
                } else if (Mathf.Approximately(pos.y, bounds.min.y)) {
                    normal = AxisDirection.NegY;
                } else if (Mathf.Approximately(pos.y, bounds.max.y)) {
                    normal = AxisDirection.PosY;
                } else if (Mathf.Approximately(pos.z, bounds.min.z)) {
                    normal = AxisDirection.NegZ;
                } else {
                    normal = AxisDirection.PosZ;
                }

                return true;
            }

            normal = (AxisDirection)byte.MaxValue;
            return false;
        }

        public static quaternion fromToRotationAlongAxis(float3 from, float3 to, float3 axis) {
            from = from.projectplane(axis);
            to = to.projectplane(axis);
            return quaternion.AxisAngle(axis, mathx.signedangle(from, to, axis));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float min(float a, float b, float c) => math.min(math.min(a, b), c);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float max(float a, float b, float c) => math.max(math.max(a, b), c);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int cBitOr(this int3 value) => value.x | value.y | value.z;
    }
}