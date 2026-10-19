Shader "LiminalDoors/Evening Sky" {
 // Claude, 2026-10-18. A skybox for Liminal Doors' outdoor stops (first used by stop 02, the Faraway Field):
 // a four-colour evening gradient, long soft cloud streaks low in the sky that drift very slowly, and a low sun
 // with a wide warm halo. Written new for the fresh base; no textures, cheap enough for VR.
 Properties {
  _Top("Overhead", Color) = (0.38,0.30,0.68,1)
  _Middle("Higher sky", Color) = (0.70,0.52,0.86,1)
  _Horizon("Horizon", Color) = (1,0.74,0.62,1)
  _Ground("Below the horizon", Color) = (0.60,0.48,0.66,1)
  _Streaks("Cloud streaks", Color) = (1,0.86,0.92,1)
  _StreakAmount("Cloud streak amount", Range(0,1)) = 0.6
  _SunDir("Sun direction", Vector) = (0.12,0.04,1,0)
  _SunColor("Sun glow", Color) = (1,0.86,0.66,1)
  // Claude, 2026-10-19 (Astra: "get the sparkles in the background" from the Pink Universe world, in our own
  // version): a starry sparkle layer that twinkles, thickest overhead and gone near the sun and the horizon, a few
  // big four-point glints, and a faint pink-violet nebula behind them. All zero = the plain evening sky as before.
  [HDR] _StarColor("Sparkle colour", Color) = (1.6,1.3,1.8,1)
  _StarAmount("Sparkles", Range(0,1)) = 0
  _GlintAmount("Big glints", Range(0,1)) = 0
  _Nebula("Nebula colour", Color) = (0.62,0.30,0.72,1)
  _NebulaAmount("Nebula", Range(0,1)) = 0
 }
 SubShader {
  Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
  Cull Off ZWrite Off
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   float4 _Top, _Middle, _Horizon, _Ground, _Streaks, _SunDir, _SunColor; float _StreakAmount;
   float4 _StarColor, _Nebula; float _StarAmount, _GlintAmount, _NebulaAmount;
   float Hash3(float3 p) { return frac(sin(dot(p, float3(127.1, 311.7, 74.7))) * 43758.5453); }
   // one sparkle per lit cell of a 3D grid laid over the sky sphere: x = brightness, y = the cell's own random
   float2 Sparkle(float3 d, float scale, float density, float size, float speed) {
    float3 q = d * scale, cell = floor(q), f = frac(q) - 0.5;
    float r = Hash3(cell);
    if (r < 1 - density) return 0;
    float3 off = (float3(Hash3(cell + 17.3), Hash3(cell + 41.9), Hash3(cell + 83.1)) - 0.5) * 0.55;
    float dist = length(f - off);
    float tw = 0.5 + 0.5 * sin(_Time.y * speed * (0.6 + r * 1.8) + r * 91.0);
    tw = tw * tw * (0.35 + 0.65 * Hash3(cell + 5.1));
    return float2(saturate(1 - dist / size) * tw, r);
   }
   struct appdata { float4 vertex:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
   struct v2f { float4 pos:SV_POSITION; float3 dir:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
   float Hash(float2 p) { return frac(sin(dot(p, float2(41.3, 289.1))) * 15731.743); }
   float Noise(float2 p) {
    float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f);
    return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x), lerp(Hash(i + float2(0, 1)), Hash(i + 1), f.x), f.y);
   }
   v2f vert(appdata v) {
    v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos = UnityObjectToClipPos(v.vertex); o.dir = v.vertex.xyz; return o;
   }
   half4 frag(v2f i) : SV_Target {
    float3 d = normalize(i.dir); float h = d.y;
    float3 c;
    if (h >= 0) {
     c = lerp(_Horizon.rgb, _Middle.rgb, smoothstep(0.0, 0.3, h));
     c = lerp(c, _Top.rgb, smoothstep(0.25, 0.95, h));
    } else c = lerp(_Horizon.rgb, _Ground.rgb, smoothstep(0.0, -0.2, h));
    // long streaks: stretched along the horizon, thickest a little above it
    float az = atan2(d.x, d.z);
    float n = Noise(float2(az * 5.0 + _Time.x * 0.05, h * 34.0)) * 0.65 + Noise(float2(az * 13.0 - _Time.x * 0.08, h * 80.0)) * 0.35;
    float band = smoothstep(0.015, 0.07, h) * (1 - smoothstep(0.28, 0.55, h));
    c = lerp(c, _Streaks.rgb, saturate((n - 0.52) * 3.0) * band * _StreakAmount);
    // the low sun: a wide halo and a soft bright core
    float s = saturate(dot(d, normalize(_SunDir.xyz)));
    // the sparkle sky: nebula first, then fine sparkles, then a few big glints; none near the sun or the horizon
    float up = smoothstep(0.06, 0.5, h) * (1 - pow(s, 3));
    if (_NebulaAmount > 0) {
     float2 dome = d.xz / (d.y + 0.35) * 1.8;   // laid over the dome, so it has no seam or pinch overhead
     float neb = Noise(dome + _Time.x * 0.02) * 0.6 + Noise(dome * 2.3 + 3.1) * 0.4;
     c = lerp(c, _Nebula.rgb, saturate((neb - 0.45) * 2.2) * up * _NebulaAmount);
    }
    if (_StarAmount > 0) {
     float2 a = Sparkle(d, 150, 0.22, 0.20, 2.6), b = Sparkle(d + 0.37, 290, 0.18, 0.25, 3.4), e = Sparkle(d - 0.21, 70, 0.10, 0.16, 1.9);
     float fine = a.x * a.x + b.x * b.x * 0.8 + e.x * e.x * 1.4;
     c += _StarColor.rgb * fine * up * _StarAmount;
    }
    if (_GlintAmount > 0) {
     float3 q = d * 34, cell = floor(q), f = frac(q) - 0.5;
     float r = Hash3(cell);
     if (r > 0.9) {
      float3 off = (float3(Hash3(cell + 2.3), Hash3(cell + 9.7), Hash3(cell + 4.4)) - 0.5) * 0.4;
      float3 g = abs(f - off);
      float rays = exp(-g.x * 70) * exp(-g.y * 9) + exp(-g.y * 70) * exp(-g.x * 9) + exp(-g.z * 70) * exp(-(g.x + g.y) * 6);
      float core = exp(-length(f - off) * 26);
      float tw = 0.55 + 0.45 * sin(_Time.y * (1.1 + r * 2.0) + r * 57.0);
      c += _StarColor.rgb * (rays * 0.55 + core * 1.4) * tw * up * _GlintAmount;
     }
    }
    c += _SunColor.rgb * (pow(s, 6) * 0.32 + pow(s, 90) * 0.45 + pow(s, 900) * 1.2);
    return half4(c, 1);
   }
   ENDCG
  }
 }
}
