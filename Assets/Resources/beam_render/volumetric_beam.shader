Shader "Custom/volumetric_beam" {
    Properties {
        [KeywordEnum(SINGLE_PIXEL, ACCUMULATOR_TEXTURE)] KW_MODE("KW_MODE", Integer) = 0

        [HideInInspector] uBeamDir("uBeamDir", Vector) = (1, 0, 0, 0)
        [HideInInspector] uBeamBasisX("uBeamBasisX", Vector) = (1, 0, 0, 0)
        [HideInInspector] uBeamBasisY("uBeamBasisY", Vector) = (0, 1, 0, 0)
        [HideInInspector] uBeamSize("uBeamSize", Vector) = (1, 1, 0, 0)
        [HideInInspector] uViewRayOriginOnBeamBasis("uViewRayOriginOnBeamBasis", Vector) = (0, 0, 0, 0)
        [HDR] uAccumulatorTexture("uAccumulatorTexture", 2D) = "white" {}
        [HideInInspector] uAccumulatorAngleRange("uAccumulatorAngleRange", Vector) = (0, 0, 0, 0)
        [HideInInspector] uBeamColor("uBeamColor", Vector) = (0, 0, 0, 0)
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
            // float4 uClipPlanes[4]; // xyz: normal; w: position along normal

            #ifdef KW_MODE_ACCUMULATOR_TEXTURE
            Texture2D uAccumulatorTexture;
            SamplerState LinearClampSampler;
            float2 uAccumulatorAngleRange;
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
                    value = 1.0 + (1.0 - exp(-value / range)) * range;
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

            float4 frag(Varyings IN) : SV_Target {
                float3 viewDir = -GetWorldSpaceNormalizeViewDir(IN.positionWS);
                float2 viewDirOnBeamBasis = normalize(float2(dot(viewDir, uBeamBasisX), dot(viewDir, uBeamBasisY)));
                RayRectIntersection rayRectIntersection = rayRectIntersect(
                    uViewRayOriginOnBeamBasis, viewDirOnBeamBasis, uBeamSize * 0.5);
                if (isinf(rayRectIntersection.enterDist)) return float4(1.0, 0.0, 1.0, 0.0);
                float4 beamColor = 0.0;
                #ifdef KW_MODE_ACCUMULATOR_TEXTURE
                {
                    float viewDirOnBeamPlaneAngle = atan2(viewDirOnBeamBasis.y, viewDirOnBeamBasis.x);
                    float2 uAccumulatorTextureSize;
                    uAccumulatorTexture.GetDimensions(uAccumulatorTextureSize.x, uAccumulatorTextureSize.y);
                    float2 uAccumulatorTextureHalfTexel = 0.5 / uAccumulatorTextureSize;
                    float uAccumulatorSampleV = Remap(
                        uAccumulatorAngleRange.x, uAccumulatorAngleRange.y,
                        uAccumulatorTextureHalfTexel.y, 1.0 - uAccumulatorTextureHalfTexel.y,
                        viewDirOnBeamPlaneAngle
                    );

                    float uAccumulatorSampleU = Remap(
                        0, length(uBeamSize),
                        uAccumulatorTextureHalfTexel.x, 1.0 - uAccumulatorTextureHalfTexel.x,
                        rayRectIntersection.exitDist - rayRectIntersection.enterDist
                    );
                    beamColor = uAccumulatorTexture.
                        SampleLevel(LinearClampSampler, float2(uAccumulatorSampleU, uAccumulatorSampleV), 0);
                }
                #elif defined(KW_MODE_SINGLE_PIXEL)
                {
                    beamColor = uBeamColor * (rayRectIntersection.exitDist - rayRectIntersection.enterDist);
                }
                #endif

                beamColor = saturateBeamColor(beamColor, 3.0);

                return float4(beamColor.rgb, 1.0);
            }
            ENDHLSL
        }
    }
}