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

        public static void debugDraw(this Bounds bounds, Color color, float duration = 0f, bool depthTest = true) {
            var p1 = new Vector3(bounds.min.x, bounds.min.y, bounds.min.z);
            var p2 = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
            var p3 = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
            var p4 = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
            var p5 = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
            var p6 = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
            var p7 = new Vector3(bounds.max.x, bounds.max.y, bounds.max.z);
            var p8 = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
            
            Debug.DrawLine(p1, p2, color, duration, depthTest);
            Debug.DrawLine(p2, p3, color, duration, depthTest);
            Debug.DrawLine(p3, p4, color, duration, depthTest);
            Debug.DrawLine(p4, p1, color, duration, depthTest);

            Debug.DrawLine(p5, p6, color, duration, depthTest);
            Debug.DrawLine(p6, p7, color, duration, depthTest);
            Debug.DrawLine(p7, p8, color, duration, depthTest);
            Debug.DrawLine(p8, p5, color, duration, depthTest);

            Debug.DrawLine(p1, p5, color, duration, depthTest);
            Debug.DrawLine(p2, p6, color, duration, depthTest);
            Debug.DrawLine(p3, p7, color, duration, depthTest);
            Debug.DrawLine(p4, p8, color, duration, depthTest);
        }
    }
}