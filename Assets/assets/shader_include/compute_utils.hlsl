#pragma once

#define _INTERNAL_dispatchIdToImageUV_TEMPLATE(texType) \
bool dispatchIdToImageUV(uint2 id, RWTexture2D<texType> outputImage, out float2 uv) { \
    uint2 size; \
    outputImage.GetDimensions(size.x, size.y); \
    uv = (float2(id) + 0.5) / size; \
    if (any(id >= size)) return false; \
    return true; \
}

_INTERNAL_dispatchIdToImageUV_TEMPLATE(float)
_INTERNAL_dispatchIdToImageUV_TEMPLATE(float4)