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
    c += _SunColor.rgb * (pow(s, 6) * 0.32 + pow(s, 90) * 0.45 + pow(s, 900) * 1.2);
    return half4(c, 1);
   }
   ENDCG
  }
 }
}
