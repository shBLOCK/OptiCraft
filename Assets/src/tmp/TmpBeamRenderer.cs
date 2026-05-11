using core;
using Unity.Mathematics;
using UnityEngine;

namespace tmp {
    public class TmpBeamRenderer : MonoBehaviour {
        public Beam beam;

        private MeshRenderer meshRenderer;

        private static Mesh MESH;

        [RuntimeInitializeOnLoadMethod]
        private static void CreateCube() {
            Mesh mesh = new Mesh();
            mesh.name = "GeneratedCube";

            // Cube from (0,0,0) to (1,1,1)
            // then shifted by (-0.5,-0.5,-0.5)
            //
            // UVs use the unshifted XY coordinates.

            Vector3[] vertices = {
                // Front
                new Vector3(0f, 0f, 1f) - Vector3.one * 0.5f,
                new Vector3(1f, 0f, 1f) - Vector3.one * 0.5f,
                new Vector3(1f, 1f, 1f) - Vector3.one * 0.5f,
                new Vector3(0f, 1f, 1f) - Vector3.one * 0.5f,

                // Back
                new Vector3(1f, 0f, 0f) - Vector3.one * 0.5f,
                new Vector3(0f, 0f, 0f) - Vector3.one * 0.5f,
                new Vector3(0f, 1f, 0f) - Vector3.one * 0.5f,
                new Vector3(1f, 1f, 0f) - Vector3.one * 0.5f,

                // Left
                new Vector3(0f, 0f, 0f) - Vector3.one * 0.5f,
                new Vector3(0f, 0f, 1f) - Vector3.one * 0.5f,
                new Vector3(0f, 1f, 1f) - Vector3.one * 0.5f,
                new Vector3(0f, 1f, 0f) - Vector3.one * 0.5f,

                // Right
                new Vector3(1f, 0f, 1f) - Vector3.one * 0.5f,
                new Vector3(1f, 0f, 0f) - Vector3.one * 0.5f,
                new Vector3(1f, 1f, 0f) - Vector3.one * 0.5f,
                new Vector3(1f, 1f, 1f) - Vector3.one * 0.5f,

                // Top
                new Vector3(0f, 1f, 1f) - Vector3.one * 0.5f,
                new Vector3(1f, 1f, 1f) - Vector3.one * 0.5f,
                new Vector3(1f, 1f, 0f) - Vector3.one * 0.5f,
                new Vector3(0f, 1f, 0f) - Vector3.one * 0.5f,

                // Bottom
                new Vector3(0f, 0f, 0f) - Vector3.one * 0.5f,
                new Vector3(1f, 0f, 0f) - Vector3.one * 0.5f,
                new Vector3(1f, 0f, 1f) - Vector3.one * 0.5f,
                new Vector3(0f, 0f, 1f) - Vector3.one * 0.5f,
            };

            Vector2[] uvs = new Vector2[vertices.Length];

            for (int i = 0; i < vertices.Length; i++) {
                // Undo center offset for UV generation
                Vector3 p = vertices[i] + Vector3.one * 0.5f;

                uvs[i] = new Vector2(p.x, p.y);
            }

            int[] triangles = {
                0, 1, 2, 0, 2, 3,
                4, 5, 6, 4, 6, 7,
                8, 9, 10, 8, 10, 11,
                12, 13, 14, 12, 14, 15,
                16, 17, 18, 16, 18, 19,
                20, 21, 22, 20, 22, 23
            };

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            MESH = mesh;
        }

        private void Start() {
            var child = transform.GetChild(0);
            child.GetComponent<MeshFilter>().mesh = MESH;
            meshRenderer = child.GetComponent<MeshRenderer>();
            meshRenderer.material.SetTexture("_BaseMap", beam.image.getTexture());
            meshRenderer.material.SetColor("_BaseColor", (Vector4) beam.image.tint);
        }

        private void LateUpdate() {
            var pt = beam.space.simulator.partialTick;

            float3 tailPos = beam.tailPos;
            if (!beam.wasBeingEmitted) {
                tailPos -= beam.direction.float3() * (1f - pt);
            }

            float length = beam.length;
            float lengthDelta = 0f;
            if (beam.wasBeingEmitted) lengthDelta += 1f;
            if (beam.wasBeingConsumed) lengthDelta -= 1f;
            length -= lengthDelta * (1f - pt);

            transform.localPosition = tailPos;
            transform.localScale = new float3(1f, 1f, length);
            transform.localRotation = Quaternion.FromToRotation(Vector3.forward, beam.direction.float3());
        }
    }
}