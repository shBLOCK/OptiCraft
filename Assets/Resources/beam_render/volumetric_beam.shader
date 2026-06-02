Shader "Custom/volumetric_beam" {
    Properties {
        [KeywordEnum(SINGLE_PIXEL, ACCUMULATOR_TEXTURE)] KW_MODE("KW_MODE", Integer) = 0
    }

    SubShader {
        Tags {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "ForceNoShadowCasting" = "True"
            "IgnoreProjector" = "True"
        }

        Cull Front
        //TODO: render beams to separate framebuffer (with alpha value) and blit to screen
        BlendOp Add
        Blend One One, Zero One
        ZTest Always
        ZWrite Off
        Offset -1, 0

        Pass {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local_fragment KW_MODE_SINGLE_PIXEL KW_MODE_ACCUMULATOR_TEXTURE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "beam_render_utils.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
            };

            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            float3 uBeamDir;
            float3 uBeamBasisX;
            float3 uBeamBasisY;
            float2 uBeamSize;
            float2 uViewRayOriginOnBeamBasis;
            /// Each plane is represented by 2 vectors: (position, normal)
            float3 uClipPlanes[8];
            uint uClipPlanesCount;

            #ifdef KW_MODE_ACCUMULATOR_TEXTURE
            Texture2D uAccumulatorTexture;
            SamplerState LinearClampSampler;
            SamplerState PointClampSampler;
            float2 uViewRayAngleRange;
            float uViewRayAngleRangeOffset; // offset of the angle range from base range of [-pi, pi]
            #endif

            #ifdef KW_MODE_SINGLE_PIXEL
            float4 uBeamColor;
            #endif

            Varyings vert(Attributes IN) {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                return OUT;
            }

            float saturateBeamColor(float value, float range) {
                float sgn = sign(value);
                value = abs(value);
                if (value > 1.0) {
                    value = 1.0 + (1.0 - exp(-(value - 1.0) / range)) * range;
                }
                return value * sgn;
            }

            float4 saturateBeamColor(float4 value, float range) {
                return float4(
                    saturateBeamColor(value.x, range),
                    saturateBeamColor(value.y, range),
                    saturateBeamColor(value.z, range),
                    saturateBeamColor(value.w, range)
                );
            }

            float sqr(float x) { return x * x; }

            float4 frag(Varyings IN) : SV_Target {
                float3 viewOrigin = GetCurrentViewPosition();
                float3 viewDir = normalize(IN.positionWS - viewOrigin);
                float2 viewRayOnBeamBasis = normalize(float2(dot(viewDir, uBeamBasisX), dot(viewDir, uBeamBasisY)));
                RayRectIntersection rayRectIntersection = rayRectIntersect(
                    uViewRayOriginOnBeamBasis, viewRayOnBeamBasis, uBeamSize * 0.5);
                if (isinf(rayRectIntersection.enterDist)) discard;
                // depth here means DISTANCE to something, not view-space Z value!
                // "2D depth": measured on the cross-section plane of the beam
                // eye depth: zero point is at camera pos (normal depth)
                // beam depth: zero point is the point at which view ray enter the beam (basically this measures the penetrtaion depth into the beam)
                float depth2Dto3D = rsqrt(1.0 - sqr(dot(viewDir, uBeamDir)));
                depth2Dto3D = min(depth2Dto3D, 1e30);
                float eyeDepth3D_sceneDepth = LinearEyeDepth(
                    LoadSceneDepth(uint2(IN.positionHCS.xy)),
                    _ZBufferParams
                ) / dot(viewDir, GetViewForwardDir());
                float eyeDepth3D_beamEnter = max(0.0, rayRectIntersection.enterDist) * depth2Dto3D;
                if (eyeDepth3D_sceneDepth < eyeDepth3D_beamEnter) discard;
                float eyeDepth3D_beamExit = rayRectIntersection.exitDist * depth2Dto3D;
                eyeDepth3D_beamExit = min(eyeDepth3D_sceneDepth, eyeDepth3D_beamExit);

                // clip planes
                for (uint i = 0; i < min(4,uClipPlanesCount * 2); i += 2) {
                    float3 planePos = uClipPlanes[i];
                    float3 planeNormal = uClipPlanes[i + 1];
                    float backFrontSign = sign(dot(planeNormal, viewDir)); // 1 for back, -1 for front
                    float eyeDepth3D_planeIntersection =
                        dot(planePos - viewOrigin, planeNormal) / dot(planeNormal, viewDir);
                    if (backFrontSign < 0.0) { // front
                        eyeDepth3D_beamEnter = max(eyeDepth3D_beamEnter, eyeDepth3D_planeIntersection);
                    } else { // back
                        eyeDepth3D_beamExit = min(eyeDepth3D_beamExit, eyeDepth3D_planeIntersection);
                    }
                }

                if (eyeDepth3D_beamEnter >= eyeDepth3D_beamExit) discard;

                float4 beamColor = 0.0;
                #ifdef KW_MODE_ACCUMULATOR_TEXTURE
                {
                    float viewRayOnBeamPlaneAngle = atan2(viewRayOnBeamBasis.y, viewRayOnBeamBasis.x) +
                        uViewRayAngleRangeOffset;
                    float2 uAccumulatorTextureSize;
                    uAccumulatorTexture.GetDimensions(uAccumulatorTextureSize.x, uAccumulatorTextureSize.y);
                    float2 uAccumulatorTextureHalfTexel = 0.5 / uAccumulatorTextureSize;
                    float uAccumulatorSampleV = Remap(
                        uViewRayAngleRange.x, uViewRayAngleRange.y,
                        uAccumulatorTextureHalfTexel.y, 1.0 - uAccumulatorTextureHalfTexel.y,
                        viewRayOnBeamPlaneAngle
                    );

                    float2 beamDepth2D_range = float2(0.0, length(uBeamSize));
                    float eyeDepth3DToBeamDepth3DOffset = max(0.0, rayRectIntersection.enterDist) * depth2Dto3D;
                    float beamDepth3D_beamEnter = eyeDepth3D_beamEnter - eyeDepth3DToBeamDepth3DOffset;
                    float beamDepth3D_beamExit = eyeDepth3D_beamExit - eyeDepth3DToBeamDepth3DOffset;
                    beamColor = uAccumulatorTexture.SampleLevel(
                        LinearClampSampler,
                        float2(
                            Remap(
                                beamDepth2D_range.x, beamDepth2D_range.y,
                                uAccumulatorTextureHalfTexel.x, 1.0 - uAccumulatorTextureHalfTexel.x,
                                beamDepth3D_beamExit / depth2Dto3D
                            ),
                            uAccumulatorSampleV
                        ),
                        0
                    );
                    if (beamDepth3D_beamEnter > 0.0) {
                        beamColor -= uAccumulatorTexture.SampleLevel(
                            LinearClampSampler,
                            float2(
                                Remap(
                                    beamDepth2D_range.x, beamDepth2D_range.y,
                                    uAccumulatorTextureHalfTexel.x, 1.0 - uAccumulatorTextureHalfTexel.x,
                                    beamDepth3D_beamEnter / depth2Dto3D
                                ),
                                uAccumulatorSampleV
                            ),
                            0
                        );
                    }

                    beamColor *= depth2Dto3D;
                }
                #elif defined(KW_MODE_SINGLE_PIXEL)
                {
                    beamColor = uBeamColor * (eyeDepth3D_beamExit - eyeDepth3D_beamEnter);
                }
                #endif

                // beamColor *= log(depth2Dto3D + 1.0);

                beamColor = saturateBeamColor(beamColor, 3.0);

                return float4(beamColor.rgb, 1.0);
            }
            ENDHLSL
        }
    }
}