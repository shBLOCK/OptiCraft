using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
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

        private static Material INTERNAL_COLORED_MATERIAL;
        private static Mesh CUBE_WIREFRAME_MESH;

        [RuntimeInitializeOnLoadMethod]
        private static void INIT() {
            INTERNAL_COLORED_MATERIAL = new Material(Shader.Find("Hidden/Internal-Colored"));

            CUBE_WIREFRAME_MESH = new Mesh {
                name = "Cube Wireframe"
            };

            // Unit cube centered at origin
            CUBE_WIREFRAME_MESH.vertices = new[] {
                new Vector3(-0.5f, -0.5f, -0.5f), // 0
                new Vector3(0.5f, -0.5f, -0.5f), // 1
                new Vector3(0.5f, 0.5f, -0.5f), // 2
                new Vector3(-0.5f, 0.5f, -0.5f), // 3

                new Vector3(-0.5f, -0.5f, 0.5f), // 4
                new Vector3(0.5f, -0.5f, 0.5f), // 5
                new Vector3(0.5f, 0.5f, 0.5f), // 6
                new Vector3(-0.5f, 0.5f, 0.5f), // 7
            };

            CUBE_WIREFRAME_MESH.SetIndices(new[] {
                // Bottom
                0, 1,
                1, 2,
                2, 3,
                3, 0,

                // Top
                4, 5,
                5, 6,
                6, 7,
                7, 4,

                // Vertical
                0, 4,
                1, 5,
                2, 6,
                3, 7,
            }, MeshTopology.Lines, 0);

            CUBE_WIREFRAME_MESH.RecalculateBounds();
        }

        public static void drawCubeWireframe(float4x4 transform, Color color) {
            var renderParams = new RenderParams(INTERNAL_COLORED_MATERIAL) {
                matProps = new MaterialPropertyBlock()
            };
            renderParams.matProps.SetColor("_Color", color);
            Graphics.RenderMesh(renderParams, CUBE_WIREFRAME_MESH, 0, transform);
        }
        
        public static void drawBoundsWireframe(Bounds bounds, Color color) =>
            drawCubeWireframe(Matrix4x4.TRS(bounds.center, Quaternion.identity, bounds.size), color);
    }
}