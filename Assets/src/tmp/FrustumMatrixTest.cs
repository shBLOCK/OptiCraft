using Unity.Mathematics;
using UnityEngine;

[ExecuteInEditMode]
public class FrustumMatrixTest : MonoBehaviour {
    public float4 col0 = new(1f, 0f, 0f, 0f);
    public float4 col1 = new(0f, 1f, 0f, 0f);
    public float4 col2 = new(0f, 0f, 1f, 0f);
    public float4 col3 = new(0f, 0f, 0f, 1f);

    public Material material;

    private void OnDrawGizmos() {
        var matrix = new float4x4(col0, col1, col2, col3);
        var verts = new Vector3[8] {
            new(-1f, -1f, -1f),
            new(1f, -1f, -1f),
            new(1f, 1f, -1f),
            new(-1f, 1f, -1f),
            new(-1f, -1f, 1f),
            new(1f, -1f, 1f),
            new(1f, 1f, 1f),
            new(-1f, 1f, 1f),
        };
        for (int i = 0; i < verts.Length; i++) {
            float4 vert = new float4(verts[i], 1f);
            float4 vecw = math.mul(matrix, vert);
            verts[i] = vecw.xyz / vecw.w;
        }

        var FACE_COLORS = new[] { Color.red, Color.yellow, Color.green, Color.cyan };

        for (int i = 0; i < 4; i++) {
            int a = i;
            int b = (i + 1) % 4;
            int c = a + 4;
            int d = b + 4;
            var mesh = new Mesh {
                vertices = verts,
                triangles = new[] { a, b, c, c, b, d }
            };
            material.SetColor("_BaseColor", FACE_COLORS[i]);
            material.SetPass(0);
            Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
            Gizmos.color = Color.black;
            Gizmos.DrawLineStrip(new[] { verts[a], verts[b], verts[d], verts[c] }, true);
        }
    }
}