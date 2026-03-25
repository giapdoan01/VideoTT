#ifndef CLOUD_SHADOW_INCLUDED
#define CLOUD_SHADOW_INCLUDED

// Populated each frame by CloudShadowSystem.cs via Shader.SetGlobal*
TEXTURE2D(_CloudShadowTex);
SAMPLER(sampler_CloudShadowTex);

float4 _CloudShadowAreaCenter;   // xz = world center of shadow map coverage
float4 _CloudShadowAreaExtents;  // xz = half-size in world units

// Returns accumulated cloud shadow density [0, 1].
// 0 = no cloud shadow, 1 = fully covered (multiple overlapping clouds accumulate).
float SampleCloudShadowDensity(float3 worldPos)
{
    float2 offset = float2(worldPos.x - _CloudShadowAreaCenter.x,
                           worldPos.z - _CloudShadowAreaCenter.z);
    float2 uv = offset / _CloudShadowAreaExtents.xz + 0.5;

    // outside shadow map coverage = no shadow
    if (any(uv < 0.0) || any(uv > 1.0)) return 0.0;

    return saturate(SAMPLE_TEXTURE2D(_CloudShadowTex, sampler_CloudShadowTex, uv).r);
}

// Returns light attenuation [0, 1].
// 1 = full light (no cloud), 0 = fully in cloud shadow.
// strength controls overall max darkness of cloud shadows.
half GetCloudShadowAttenuation(float3 worldPos, half strength)
{
    float density = SampleCloudShadowDensity(worldPos);
    return 1.0h - (half)density * strength;
}

#endif
