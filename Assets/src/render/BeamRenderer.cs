using System;
using System.Collections.Generic;
using core;
using core.beam;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using utils;

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

        private (float2 range, bool invertX) calcAngleRange(
            ReadOnlySpan<float2> corners,
            float2 viewRayOriginOnBeamBasis
        ) {
            if ((viewRayOriginOnBeamBasis.abs() <= beamSize * 0.5f).all()) {
                // TODO: more aggressive (smaller) angle range
                return (range: new float2(-math.PI, math.PI), invertX: false);
            }

            bool nxny = false, nxpy = false;
            Span<float2> cornerRays = stackalloc float2[4];
            for (int i = 0; i < 4; i++) {
                var ray = corners[i] - viewRayOriginOnBeamBasis;
                if (ray.x <= 0f) {
                    nxny |= ray.y < 0f;
                    nxpy |= ray.y >= 0f;
                }

                cornerRays[i] = ray;
            }

            float2 range = new(float.PositiveInfinity, float.NegativeInfinity);
            var invertX = nxny && nxpy;
            var xSign = invertX ? -1f : 1f;
            for (int i = 0; i < 4; i++) {
                var ray = cornerRays[i];
                ray.x *= xSign;
                var angle = math.atan2(ray.y, ray.x);
                range.x = math.min(range.x, angle);
                range.y = math.max(range.y, angle);
            }

            return (range, invertX);
        }

        private static int uBeamImage = Shader.PropertyToID("uBeamImage");
        private static int uBeamImageOrigin = Shader.PropertyToID("uBeamImageOrigin");
        private static int uBeamImageSize = Shader.PropertyToID("uBeamImageSize");
        private static int uBeamImageModulation = Shader.PropertyToID("uBeamImageModulation");
        private static int uBeamImageBias = Shader.PropertyToID("uBeamImageBias");
        private static int uAccumulatorTexture = Shader.PropertyToID("uAccumulatorTexture");
        private static int uViewRayOriginOnBeamBasis = Shader.PropertyToID("uViewRayOriginOnBeamBasis");
        private static int uViewRayAngleRange = Shader.PropertyToID("uViewRayAngleRange");
        private static int uViewRayAngleRangeInvertX = Shader.PropertyToID("uViewRayAngleRangeInvertX");
        private static int uBeamDir = Shader.PropertyToID("uBeamDir");
        private static int uBeamBasisX = Shader.PropertyToID("uBeamBasisX");
        private static int uBeamBasisY = Shader.PropertyToID("uBeamBasisY");
        private static int uBeamSize = Shader.PropertyToID("uBeamSize");
        private static int uBeamColor = Shader.PropertyToID("uBeamColor");
        private static int uClipPlanes = Shader.PropertyToID("uClipPlanes");
        private static int uClipPlanesCount = Shader.PropertyToID("uClipPlanesCount");

        private void LateUpdate() {
            var cam = Camera.main;
            var mouseRay = cam.ScreenPointToRay(inputActions.UI.Point.ReadValue<Vector2>());
            var camPos = new float3(cam.transform.position);
            var camPlanes = GeometryUtility.CalculateFrustumPlanes(cam);

            hoveringBeam = null;
            Bounds? hoveringBounds = null;

            int _tmp_accumulatorTextureIndex = 0;

            var corners = new[] {
                new float2(-0.5f, -0.5f) * beamSize,
                new float2(0.5f, -0.5f) * beamSize,
                new float2(-0.5f, 0.5f) * beamSize,
                new float2(0.5f, 0.5f) * beamSize
            };

            var singlePixelRenderParams = new RenderParams(SINGLE_PIXEL_BEAM_MAT)
                { matProps = new MaterialPropertyBlock() };
            var accTexRenderParams = new RenderParams(ACCUMULATOR_TEXTURE_BEAM_MAT)
                { matProps = new MaterialPropertyBlock() };

            // work around Unity limitation
            singlePixelRenderParams.matProps.SetVectorArray(uClipPlanes, new Vector4[8]);
            accTexRenderParams.matProps.SetVectorArray(uClipPlanes, new Vector4[8]);
            var uClipPlanesBuffer = new List<Vector4>(8);

            var simSpace = simulator.rootSpace;
            foreach (var beam in simSpace.enumerateBeams()) {
                var basis = beam.image.orientation.basis(beam.direction.axis());
                float3 tailPos = beam.tailPos;
                float3 headPos = beam.headPos;
                if (!beam.beingEmitted) tailPos -= beam.direction.float3();
                if (!beam.wasBeingEmitted) tailPos -= beam.direction.float3() * (1f - simulator.partialTick);
                if (!beam.beingConsumed) headPos -= beam.direction.float3();
                if (!beam.wasBeingConsumed) headPos -= beam.direction.float3() * (1f - simulator.partialTick);

                uClipPlanesBuffer.Clear();

                var headBoundsOffset = 0f;
                var headDevice = simSpace.getDeviceAt(beam.headPos);
                if (headDevice != null) {
                    headDevice.beamRendering_configureBeamEnd(
                        beam, Beam.End.Head, beam.direction, headPos,
                        uClipPlanesBuffer, out headBoundsOffset
                    );
                } else {
                    uClipPlanesBuffer.Add(headPos.f4());
                    uClipPlanesBuffer.Add(beam.direction.float3().f4());
                }
                
                var tailBoundsOffset = 0f;
                var tailDevicePos = beam.tailPos;
                if (!beam.beingEmitted) tailDevicePos -= beam.direction.int3();
                if (!beam.wasBeingEmitted && beam.wasWasBeingEmitted) tailDevicePos -= beam.direction.int3();
                var tailDevice = (tailDevicePos != beam.headPos).any() ? simSpace.getDeviceAt(tailDevicePos) : null;
                if (tailDevice != null) {
                    tailDevice.beamRendering_configureBeamEnd(
                        beam, Beam.End.Tail, beam.direction.opposite(), tailPos,
                        uClipPlanesBuffer, out tailBoundsOffset
                    );
                } else {
                    uClipPlanesBuffer.Add(tailPos.f4());
                    uClipPlanesBuffer.Add(beam.direction.opposite().float3().f4());
                }

                var boundsTail = tailPos + beam.direction.float3(-tailBoundsOffset);
                var boundsHead = headPos + beam.direction.float3(headBoundsOffset);
                if ((boundsTail == boundsHead).all()) continue;
                var bounds = new Bounds(
                    (boundsHead + boundsTail) / 2f,
                    math.abs((boundsHead - boundsTail) + basis.x.float3(beamSize) + basis.y.float3(beamSize))
                );

                if (!GeometryUtility.TestPlanesAABB(camPlanes, bounds)) continue;

                if (bounds.IntersectRay(mouseRay)) {
                    hoveringBeam = beam;
                    hoveringBounds = bounds;
                }

                ref var renderParams = ref beam.image.isSinglePixel
                    ? ref singlePixelRenderParams
                    : ref accTexRenderParams;

                renderParams.matProps.SetVector(uBeamDir, beam.direction.float3().f4());
                renderParams.matProps.SetVector(uBeamSize, new float2(beamSize).f4());
                renderParams.matProps.SetVector(uBeamBasisX, basis.x.float3().f4());
                renderParams.matProps.SetVector(uBeamBasisY, basis.y.float3().f4());
                var beamToCam = camPos - tailPos;
                var viewRayOriginOnBeamBasis = new float2(
                    beamToCam.dot(basis.x.float3()), beamToCam.dot(basis.y.float3())
                );
                renderParams.matProps.SetVector(uViewRayOriginOnBeamBasis, viewRayOriginOnBeamBasis.f4());
                renderParams.matProps.SetVectorArray(uClipPlanes, uClipPlanesBuffer);
                renderParams.matProps.SetInt(uClipPlanesCount, uClipPlanesBuffer.Count / 2);

                if (beam.image.isSinglePixel) {
                    singlePixelRenderParams.matProps.SetVector(uBeamColor, beam.image.modulation);
                } else {
                    ACCUMULATE_CS.SetTexture(ACCUMULATE_CSK, uBeamImage,
                        beam.image.getTexture(simulator.beamImageDataManager));
                    ACCUMULATE_CS.SetInts(uBeamImageOrigin, beam.image.offset.x, beam.image.offset.y);
                    var beamImageSizeOrientated = beam.image.orientation.isXYSwapped() ? beam.image.size.yx : beam.image.size;
                    ACCUMULATE_CS.SetInts(uBeamImageSize, (int)beamImageSizeOrientated.x, (int)beamImageSizeOrientated.y);
                    ACCUMULATE_CS.SetVector(uBeamImageModulation, beam.image.modulation);
                    ACCUMULATE_CS.SetVector(uBeamImageBias, beam.image.bias);

                    if (_tmp_accumulatorTextures.Count <= _tmp_accumulatorTextureIndex) {
                        // shit
                        var rt = new RenderTexture(new RenderTextureDescriptor(
                            64, 256, RenderTextureFormat.ARGBFloat
                        )) {
                            enableRandomWrite = true
                        };
                        rt.Create();
                        _tmp_accumulatorTextures.Add(rt);
                    }

                    var accumulatorTexture = _tmp_accumulatorTextures[_tmp_accumulatorTextureIndex++];
                    accumulatorTexture.DiscardContents();
                    ACCUMULATE_CS.SetTexture(ACCUMULATE_CSK, uAccumulatorTexture, accumulatorTexture);

                    ACCUMULATE_CS.SetVector(uViewRayOriginOnBeamBasis, viewRayOriginOnBeamBasis.f4());
                    var angleRange = calcAngleRange(corners, viewRayOriginOnBeamBasis);

                    ACCUMULATE_CS.SetVector(uViewRayAngleRange, new float4(angleRange.range, 0, 0));
                    ACCUMULATE_CS.SetBool(uViewRayAngleRangeInvertX, angleRange.invertX);
                    ACCUMULATE_CS.SetVector(uBeamSize, new float2(beamSize).f4());
                    uint groupSize;
                    ACCUMULATE_CS.GetKernelThreadGroupSizes(ACCUMULATE_CSK, out _, out groupSize, out _);
                    ACCUMULATE_CS.Dispatch(
                        ACCUMULATE_CSK,
                        1,
                        (int)((uint)accumulatorTexture.height).ceilDiv(groupSize),
                        1
                    );

                    renderParams.matProps.SetTexture(uAccumulatorTexture, accumulatorTexture);
                    renderParams.matProps.SetVector(uViewRayAngleRange, new float4(angleRange.range, 0, 0));
                    renderParams.matProps.SetInt(uViewRayAngleRangeInvertX, angleRange.invertX ? 1 : 0);
                }

                var boundsSize = bounds.size;
                var matrix = new Matrix4x4(
                    new float4(boundsSize.x, 0f, 0f, 0f),
                    new float4(0f, boundsSize.y, 0f, 0f),
                    new float4(0f, 0f, boundsSize.z, 0f),
                    new float4(bounds.center, 1f)
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
                    print(hoveringBeam.Value.image.orientation);
                }
            }
        }

        private static Mesh MESH;

        [RuntimeInitializeOnLoadMethod]
        private static void CreateCube() {
            var mesh = new Mesh();

            mesh.vertices = new[] {
                new Vector3(-0.5f, -0.5f, -0.5f), // 0
                new Vector3(0.5f, -0.5f, -0.5f), // 1
                new Vector3(0.5f, 0.5f, -0.5f), // 2
                new Vector3(-0.5f, 0.5f, -0.5f), // 3
                new Vector3(-0.5f, -0.5f, 0.5f), // 4
                new Vector3(0.5f, -0.5f, 0.5f), // 5
                new Vector3(0.5f, 0.5f, 0.5f), // 6
                new Vector3(-0.5f, 0.5f, 0.5f), // 7
            };

            mesh.triangles = new[] {
                // Front (+Z)
                4, 5, 6,
                4, 6, 7,

                // Back (-Z)
                0, 2, 1,
                0, 3, 2,

                // Left (-X)
                0, 7, 3,
                0, 4, 7,

                // Right (+X)
                1, 6, 5,
                1, 2, 6,

                // Top (+Y)
                3, 6, 2,
                3, 7, 6,

                // Bottom (-Y)
                0, 5, 4,
                0, 1, 5,
            };

            mesh.RecalculateBounds();

            MESH = mesh;
        }
    }
}