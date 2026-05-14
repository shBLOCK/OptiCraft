using System;
using core;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using utils;

namespace render {
    public class BeamRenderer : MonoBehaviour {
        private static readonly Material MATERIAL = Resources.Load<Material>("src/render/Beam.mat");
        private Simulator simulator;
        private CommandBuffer cmds = new();
        private InputSystem_Actions inputActions;

        private void Awake() {
            simulator = GetComponent<Simulator>();
            inputActions = new();
            inputActions.Enable();
        }

        private Beam? hoveringBeam;
        
        private void LateUpdate() {
            var mouseRay = Camera.main.ScreenPointToRay(inputActions.UI.Point.ReadValue<Vector2>());
            hoveringBeam = null;
            
            foreach (var beam in simulator.rootSpace.enumerateBeams()) {
                var xy = beam.direction.axis().orthoAxes();
                float3 tailPos = beam.tailPos;
                if (!beam.beingEmitted) {
                    tailPos -= beam.direction.float3() * (1f - simulator.partialTick);
                }

                float length = beam.length;
                float lengthDelta = 0f;
                if (beam.beingEmitted) lengthDelta += 1f;
                if (beam.beingConsumed) lengthDelta -= 1f;
                length -= lengthDelta * (1f - simulator.partialTick);
                
                {
                    var bounds = new Bounds(
                        tailPos + beam.direction.float3(length),
                        xy.Item1.float3() + xy.Item2.float3() + beam.direction.axis().float3(length)
                    );
                    if (bounds.IntersectRay(mouseRay)) {
                        hoveringBeam = beam;
                    }
                }
                
                var matrix = new Matrix4x4(
                    new float4(xy.Item1.float3(), 0f),
                    new float4(xy.Item2.float3(), 0f),
                    new float4(beam.direction.float3(length), 0f),
                    new float4(tailPos, 1f)
                );
                var matProps = new MaterialPropertyBlock();
                matProps.SetTexture("_BaseMap", beam.image.getTexture(simulator.beamImageDataManager));
                matProps.SetColor("_BaseColor", (Vector4)beam.image.modulation);
                cmds.DrawMesh(MESH, matrix, MATERIAL, 0, -1, matProps);
            }

            Graphics.ExecuteCommandBuffer(cmds);
            cmds.Clear();
        }
        
        private void OnGUI() {
            if (Event.current.type == EventType.Repaint) {
                if (hoveringBeam != null) {
                    // GUILayout.Label("Bea");
                    GUI.DrawTexture(
                        new Rect(Screen.width - 10 - 256, 10, 256, 256),
                        hoveringBeam.Value.image.getTexture(simulator.beamImageDataManager),
                        ScaleMode.ScaleToFit,
                        false,
                        0,
                        (Vector4)hoveringBeam.Value.image.modulation,
                        Vector4.zero,
                        0
                    );
                }
            }
        }

        private static Mesh MESH;

        [RuntimeInitializeOnLoadMethod]
        private static void CreateCube() {
            Mesh mesh = new Mesh();
            mesh.name = "GeneratedCube";

            // Cube from (0,0,0) to (1,1,1)
            // then shifted by (-0.5,-0.5,-0.5)
            //
            // UVs use the unshifted XY coordinates.

            var offset = new Vector3(-0.5f, -0.5f, 0.0f);

            Vector3[] vertices = {
                // Front
                new Vector3(0f, 0f, 1f) + offset,
                new Vector3(1f, 0f, 1f) + offset,
                new Vector3(1f, 1f, 1f) + offset,
                new Vector3(0f, 1f, 1f) + offset,

                // Back
                new Vector3(1f, 0f, 0f) + offset,
                new Vector3(0f, 0f, 0f) + offset,
                new Vector3(0f, 1f, 0f) + offset,
                new Vector3(1f, 1f, 0f) + offset,

                // Left
                new Vector3(0f, 0f, 0f) + offset,
                new Vector3(0f, 0f, 1f) + offset,
                new Vector3(0f, 1f, 1f) + offset,
                new Vector3(0f, 1f, 0f) + offset,

                // Right
                new Vector3(1f, 0f, 1f) + offset,
                new Vector3(1f, 0f, 0f) + offset,
                new Vector3(1f, 1f, 0f) + offset,
                new Vector3(1f, 1f, 1f) + offset,

                // Top
                new Vector3(0f, 1f, 1f) + offset,
                new Vector3(1f, 1f, 1f) + offset,
                new Vector3(1f, 1f, 0f) + offset,
                new Vector3(0f, 1f, 0f) + offset,

                // Bottom
                new Vector3(0f, 0f, 0f) + offset,
                new Vector3(1f, 0f, 0f) + offset,
                new Vector3(1f, 0f, 1f) + offset,
                new Vector3(0f, 0f, 1f) + offset,
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
    }
}