// Claude, 2026-10-18. Shared by the Faraway Field shaders (Liminal Doors stop 02).
// "Wind moves in broad bands across the field" (floor register): one travelling band function bends the grass and
// brightens the ground under the same bands, so both move together. Positions are the stop's own (object space).
#ifndef LIMINAL_FIELD_INCLUDED
#define LIMINAL_FIELD_INCLUDED
float4 _WindDir;      // xy = direction across the field
float _WindScale;     // bands per metre (0.035 = a band every ~28 m)
float _WindSpeed;
float4 _SunDir, _Glow, _FadeColor;
float _FadeStart, _FadeEnd;

float WindBand(float2 p)
{
    float s = sin(dot(p, normalize(_WindDir.xy)) * _WindScale * 6.2832 - _Time.y * _WindSpeed);
    s = s * 0.5 + 0.5;
    return s * s * s;
}

float FieldHash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
float FieldNoise(float2 p)
{
    float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f);
    return lerp(lerp(FieldHash(i), FieldHash(i + float2(1, 0)), f.x), lerp(FieldHash(i + float2(0, 1)), FieldHash(i + 1), f.x), f.y);
}

// far things melt into the evening haze
float3 FieldFade(float3 c, float3 world)
{
    float f = saturate((distance(world, _WorldSpaceCameraPos) - _FadeStart) / max(1, _FadeEnd - _FadeStart));
    return lerp(c, _FadeColor.rgb, f * f);
}

// light coming through the grass when you look toward the low sun
float FieldBacklight(float3 world)
{
    float3 view = normalize(world - _WorldSpaceCameraPos);
    return pow(saturate(dot(view, normalize(_SunDir.xyz))), 4);
}
#endif
