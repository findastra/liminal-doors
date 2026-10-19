Shader "LiminalDoors/Pool Window" {
 // Claude, 2026-10-19. The tall windows along Pool After Closing (stop 03) that look straight into deep water: bright
 // turquoise near the top fading to blue below, light shafts swaying down from the surface, a caustic net on the glass
 // and slow bubbles rising. Also used, with _Mode 1, for the skylight: the same water seen from underneath, in pink
 // and violet, rippling overhead. Emissive (HDR) so the bloom gives it a glow. The quad's uv spans the whole pane.
 Properties {
  [HDR] _Top("Near the surface", Color) = (0.55,1.6,1.7,1)
  [HDR] _Bottom("Deep", Color) = (0.05,0.25,0.55,1)
  [HDR] _Light("Light shafts and caustics", Color) = (0.9,1.6,1.6,1)
  _Size("Pane size (m)", Vector) = (4,3.2,0,0)
  _Mode("0 = window, 1 = skylight", Float) = 0
  _Bubbles("Bubbles", Range(0,1)) = 1
  _HazeColor("Haze colour", Color) = (0.95,0.72,0.66,1)
  _HazeStart("Haze start (m)", Float) = 0
  _HazeEnd("Haze full (m, 0 = off)", Float) = 0
 }
 SubShader {
  Tags { "RenderType"="Opaque" "Queue"="Geometry" }
  Cull Off
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_instancing
   #include "UnityCG.cginc"
   #include "LiminalPool.cginc"
   float4 _Top, _Bottom, _Light, _Size; float _Mode, _Bubbles;
   struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
   struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float3 world:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
   v2f vert(appdata v) {
    v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.world = mul(unity_ObjectToWorld, v.vertex).xyz; return o;
   }
   half3 Bubbles(float2 m, float t) {
    float b = 0;
    for (int k = 0; k < 3; k++) {
     float2 cs = float2(0.45 + k * 0.2, 0.6 + k * 0.25);
     float2 q = m / cs; q.y -= t * (0.35 + k * 0.12);
     float2 cell = floor(q), f = frac(q) - 0.5;
     float r = PoolHash(cell + k * 13.1);
     if (r > 0.72) {
      f.x += sin(t * 2.0 + r * 30.0) * 0.12;
      float rad = 0.05 + 0.09 * PoolHash(cell + 7.7);
      float d = length(f) / rad;
      b += (smoothstep(1.0, 0.75, d) - smoothstep(0.75, 0.4, d) * 0.6) * (0.6 + 0.4 * r);
     }
    }
    return saturate(b);
   }
   half4 frag(v2f i) : SV_Target {
    float2 m = i.uv * _Size.xy; float t = _Time.y;
    float3 c;
    if (_Mode < 0.5) {
     c = lerp(_Bottom.rgb, _Top.rgb, smoothstep(0.0, 1.0, i.uv.y));
     float shafts = PoolNoise(float2(m.x * 1.4 + (1 - i.uv.y) * 0.8 + sin(t * 0.3) * 0.6, t * 0.15));
     shafts = pow(shafts, 3) * smoothstep(0.1, 0.95, i.uv.y);
     c += _Light.rgb * shafts * 0.45;
     c += _Light.rgb * Caustics(m * 0.7, t) * 0.25 * smoothstep(0.3, 1.0, i.uv.y);
     c += _Light.rgb * Bubbles(m + float2(i.world.z, 0), t) * 0.6 * _Bubbles;
    } else {
     float2 p = i.world.xz;
     float wave = PoolNoise(p * 0.5 + t * 0.12) * 0.6 + PoolNoise(p * 1.2 - t * 0.18) * 0.4;
     c = lerp(_Bottom.rgb, _Top.rgb, wave);
     c += _Light.rgb * Caustics(p * 0.55, t * 0.7) * 0.9;
    }
    return half4(PoolHaze(c, i.world), 1);
   }
   ENDCG
  }
 }
}
