#pragma once

#define DECLARE_BEAM_IMAGE(name) \
    uniform Texture2D name; \
    uniform float4 name##TransformPacked; \
    uniform float2 name##Offset; \
    uniform float4 name##Modulation; \
    uniform float4 name##Bias;

#define sampleBeamImage(name, uv, samplar) _sampleBeamImage( \
    name, samplar, \
    uv, \
    transpose(float3x2(name##TransformPacked.xy, name##TransformPacked.zw, name##Offset)), \
    name##Modulation, name##Bias \
)

float4 _sampleBeamImage(
    Texture2D image, SamplerState samplar,
    float2 uv,
    float2x3 transform,
    float4 modulation, float4 bias
) {
    uv = mul(transform, float3(uv, 1.0));
    return image.SampleLevel(samplar, uv, 0) * modulation + bias;
}
