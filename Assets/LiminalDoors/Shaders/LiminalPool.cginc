// Claude, 2026-10-19. Shared by the Pool After Closing shaders (Liminal Doors stop 03). Written new for this project.
// Caustics: the bright net of light that moving water throws on tiles. Here it is a few crossing ripple waves, each one
// bent by the next, folded with abs() and sharpened, so thin bright lines wander and meet like the real thing.
#ifndef LIMINAL_POOL_INCLUDED
#define LIMINAL_POOL_INCLUDED

float PoolHash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
float PoolNoise(float2 p)
{
    float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f);
    return lerp(lerp(PoolHash(i), PoolHash(i + float2(1, 0)), f.x), lerp(PoolHash(i + float2(0, 1)), PoolHash(i + 1), f.x), f.y);
}

// p in metres, t in seconds; returns 0..1, mostly dark with thin bright lines
float Caustics(float2 p, float t)
{
    float2 q = p * 1.7;
    float w = 0;
    w += sin(q.x * 1.00 + q.y * 0.35 + t * 0.9 + sin(q.y * 0.8 - t * 0.7) * 1.6);
    w += sin(q.x * -0.42 + q.y * 1.10 - t * 1.1 + sin(q.x * 0.9 + t * 0.5) * 1.6);
    w += sin(q.x * 0.71 - q.y * 0.83 + t * 0.7 + sin((q.x + q.y) * 0.6 - t * 0.9) * 1.4);
    float a = 1 - saturate(abs(w) / 1.2);
    float c = pow(a, 7);
    // a second, finer net drifting the other way
    float2 r = p * 3.1 + float2(t * 0.15, -t * 0.11);
    float v = sin(r.x + sin(r.y * 1.3 + t) * 1.2) + sin(r.y * 1.1 - sin(r.x * 0.9 - t * 1.2) * 1.2);
    c += pow(1 - saturate(abs(v) / 1.0), 9) * 0.5;
    return saturate(c);
}

// tile coordinates for a world position on a face with this normal
float2 TileUV(float3 world, float3 n)
{
    float3 a = abs(n);
    if (a.y > 0.5) return world.xz;
    if (a.x > 0.5) return world.zy;
    return world.xy;
}
#endif
