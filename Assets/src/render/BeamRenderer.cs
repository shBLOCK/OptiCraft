using System.Collections.Generic;
using core;
using core.beam;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using utils;
using Vertx.Debugging;

namespace render {
    public class BeamRenderer : MonoBehaviour {
        public float beamSize = 1f;

        private static ComputeShader ACCUMULATE_CS;
        private static int ACCUMULATE_CSK;

        private static Material SINGLE_PIXEL_BEAM_MAT;
        private static Material ACCUMULATOR_TEXTURE_BEAM_MAT;

        [RuntimeInitializeOnLoadMethod]
        private static void LOAD_RES() {
            ACCUMULATE_CS = Resources.Load<ComputeShader>("beam_render/accumulate");
            ACCUMULATE_CSK = ACCUMULATE_CS.FindKernel("CSMain");
            SINGLE_PIXEL_BEAM_MAT = Resources.Load<Material>("beam_render/volumetric_beam_single_pixel");
            ACCUMULATOR_TEXTURE_BEAM_MAT = Resources.Load<Material>("beam_render/volumetric_beam_accumulator_texture");
        }

        private Simulator simulator;
        private CommandBuffer cmds;
        private InputSystem_Actions inputActions;

        private List<RenderTexture> _tmp_accumulatorTextures = new();
        
        private void Awake() {
            simulator = GetComponent<Simulator>();
            inputActions = new();
            inputActions.Enable();
            cmds = new CommandBuffer();
        }

        private Beam? hoveringBeam;

        private void LateUpdate() {
            var mouseRay = Camera.main.ScreenPointToRay(inputActions.UI.Point.ReadValue<Vector2>());
            var camPos = new float3(Camera.main.transform.position);
            hoveringBeam = null;

            Bounds? hoveringBounds = null;
            var singlePixelRenderParams = new RenderParams(SINGLE_PIXEL_BEAM_MAT)
                { matProps = new MaterialPropertyBlock() };
            var accTexRenderParams = new RenderParams(ACCUMULATOR_TEXTURE_BEAM_MAT)
                { matProps = new MaterialPropertyBlock() };
            
            int _tmp_accumulatorTextureIndex = 0;
            
            foreach (var beam in simulator.rootSpace.enumerateBeams()) {
                var basis = beam.image.orientation.basis(beam.direction.axis());
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
                        tailPos + beam.direction.float3(length / 2f),
                        basis.x.axis().float3() + basis.y.axis().float3() + beam.direction.axis().float3(length)
                    );
                    if (bounds.IntersectRay(mouseRay)) {
                        hoveringBeam = beam;
                        hoveringBounds = bounds;
                    }
                }
                
                ref var renderParams = ref beam.image.isSinglePixel
                    ? ref singlePixelRenderParams
                    : ref accTexRenderParams;
                
                renderParams.matProps.SetVector("uBeamDir", beam.direction.float3().f4());
                renderParams.matProps.SetVector("uBeamSize", new float2(beamSize, beamSize).f4());
                renderParams.matProps.SetVector("uBeamBasisX", basis.x.float3().f4());
                renderParams.matProps.SetVector("uBeamBasisY", basis.y.float3().f4());
                var beamToCam = camPos - tailPos;
                var viewRayOriginOnBeamBasis = new float2(
                    beamToCam.dot(basis.x.float3()), beamToCam.dot(basis.y.float3())
                );
                renderParams.matProps.SetVector("uViewRayOriginOnBeamBasis", viewRayOriginOnBeamBasis.f4());

                if (beam.image.isSinglePixel) {
                    singlePixelRenderParams.matProps.SetVector("uBeamColor", beam.image.modulation);
                } else {
                    ACCUMULATE_CS.SetTexture(ACCUMULATE_CSK, "uBeamImage",
                        beam.image.getTexture(simulator.beamImageDataManager));
                    var beamImageOrigin = int2.zero;
                    beamImageOrigin.x = beam.image.orientation.xSign() == Sign.Neg ? (int)beam.image.size.x : 0;
                    beamImageOrigin.y = beam.image.orientation.ySign() == Sign.Neg ? (int)beam.image.size.y : 0;
                    if (beam.image.orientation.isXYSwapped()) beamImageOrigin = beamImageOrigin.yx;
                    ACCUMULATE_CS.SetInts("uBeamImageOrigin", beamImageOrigin.x, beamImageOrigin.y);
                    var int2Basis = beam.image.orientation.int2Basis();
                    ACCUMULATE_CS.SetInts("uBeamImageBasisPacked", int2Basis.x.x, int2Basis.x.y, int2Basis.y.x,
                        int2Basis.y.y);
                    ACCUMULATE_CS.SetInts("uBeamImageSize", (int)beam.image.size.x, (int)beam.image.size.y);
                    ACCUMULATE_CS.SetVector("uBeamImageModulation", beam.image.modulation);
                    ACCUMULATE_CS.SetVector("uBeamImageBias", beam.image.bias);

                    if (_tmp_accumulatorTextures.Count <= _tmp_accumulatorTextureIndex) { // shit
                        var rt = new RenderTexture(new RenderTextureDescriptor(
                            8, 256, RenderTextureFormat.ARGBFloat
                        )) {
                            enableRandomWrite = true
                        };
                        rt.Create();
                        _tmp_accumulatorTextures.Add(rt);
                    }
                    var accumulatorTexture = _tmp_accumulatorTextures[_tmp_accumulatorTextureIndex++];
                    ACCUMULATE_CS.SetTexture(ACCUMULATE_CSK, "uAccumulatorTexture", accumulatorTexture);
                    
                    ACCUMULATE_CS.SetVector("uRayOrigin", viewRayOriginOnBeamBasis.f4());
                    var angleRange = new float2(float.PositiveInfinity, float.NegativeInfinity);
                    foreach (var corner in new float2[]{
                        new float2(-0.5f, -0.5f) * beamSize,
                        new float2(-0.5f, 0.5f) * beamSize,
                        new float2(0.5f, -0.5f) * beamSize,
                        new float2(0.5f, 0.5f) * beamSize,
                    }) {
                        var originToCorner = corner - viewRayOriginOnBeamBasis;
                        var angle = math.atan2(originToCorner.y, originToCorner.x);
                        angleRange.x = math.min(angleRange.x, angle);
                        angleRange.y = math.max(angleRange.y, angle);
                    }
                    // print(angleRange);
                    ACCUMULATE_CS.SetVector("uRayAngleRange", new float4(angleRange, 0, 0)); // TODO
                    ACCUMULATE_CS.SetVector("uBeamSize", new float4(beamSize, beamSize, 0, 0));
                    uint groupSize;
                    ACCUMULATE_CS.GetKernelThreadGroupSizes(ACCUMULATE_CSK, out _, out groupSize, out _);
                    ACCUMULATE_CS.Dispatch(
                        ACCUMULATE_CSK,
                        1,
                        (int)((uint)accumulatorTexture.height).ceilDiv(groupSize),
                        1
                    );

                    renderParams.matProps.SetTexture("uAccumulatorTexture", accumulatorTexture);
                    renderParams.matProps.SetVector("uAccumulatorAngleRange", new float4(angleRange, 0, 0));
                }

                var matrix = new Matrix4x4(
                    new float4(basis.x.float3(beamSize), 0f),
                    new float4(basis.y.float3(beamSize), 0f),
                    new float4(beam.direction.float3(length), 0f),
                    new float4(tailPos, 1f)
                );
                Graphics.RenderMesh(renderParams, MESH, 0, matrix);
            }

            if (hoveringBounds != null) {
                var bounds = hoveringBounds.Value;
                bounds.Expand(0.1f);
                DebugUtils.drawBoundsWireframe(bounds, Color.HSVToRGB(math.frac(Time.time + 0.5f), 1f, 0.8f));
            }

            // Graphics.ExecuteCommandBuffer(cmds);
            // cmds.Clear();
        }

        public void _OnGUI() {
            if (Event.current.type == EventType.Repaint) {
                if (hoveringBeam != null) {
                    GUI.DrawTexture(
                        new Rect(Screen.width - 10 - 256, 10, 256, 256),
                        hoveringBeam.Value.image.getTexture(simulator.beamImageDataManager),
                        ScaleMode.ScaleToFit,
                        false,
                        0,
                        (Vector4)(new float4(hoveringBeam.Value.image.modulation.xyz, 1f)),
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