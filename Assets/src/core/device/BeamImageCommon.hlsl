#define BeamTextureUniformDef(name) \
    Texture2D name; \
    SamplerState sampler##name; \
    float4 name##Tint;

#define SampleBeamTexture(name, uv) \
    name.SampleLevel(sampler##name, uv, 0) * name##Tint
