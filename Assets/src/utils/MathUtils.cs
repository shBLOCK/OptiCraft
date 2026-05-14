using System.Text.Json.Nodes;
using Unity.Mathematics;
using UnityEngine;

namespace utils {
    public static class MathUtils {
        public static JsonArray toJsonArray(this int3 value) => new() { value.x, value.y, value.z };

        public static int3 toInt3(this JsonArray data) =>
            new(data[0].GetValue<int>(), data[1].GetValue<int>(), data[2].GetValue<int>());

        public static bool intersectRay(this Bounds bounds, Ray ray, out AxisDirection normal) {
            if (bounds.IntersectRay(ray, out float distance)) {
                float3 pos = ray.origin + ray.direction * distance;
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
    }
}