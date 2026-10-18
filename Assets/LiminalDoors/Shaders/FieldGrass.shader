Shader "LiminalDoors/Field Grass" {
 // Claude, 2026-10-18. Long grass for the Faraway Field (stop 02). Each blade is one triangle; uv.y is 0 at the root and
 // 1 at the tip, the vertex colour is the blade's colour (linear). Blades bend with the wind bands, sway a little on
 // their own, glow rose when you look toward the low sun, and fade into the haze far off. Unlit, no textures.
 Properties {
  _Tint("Tint", Color) = (1,1,1,1)
  _Bend("Bend in a gust (m)", Float) = 0.3
  _WindDir("Wind direction (xy)", Vector) = (0.8,0.6,0,0)
  _WindScale("Wind bands per metre", Float) = 0.035
  _WindSpeed("Wind speed", Float) = 1.1
  _SunDir("Sun direction", Vector) = (0.12,0.04,1,0)
  _Glow("Backlight glow", Color) = (1,0.62,0.72,1)
  _FadeColor("Haze colour", Color) = (0.86,0.66,0.74,1)
  _FadeStart("Haze start (m)", Float) = 45
  _FadeEnd("Haze full (m)", Float) = 190
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
   #include "LiminalField.cginc"
   float4 _Tint; float _Bend;
   struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
   struct v2f { float4 pos:SV_POSITION; float4 color:COLOR; float3 world:TEXCOORD0; float2 hb:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
   v2f vert(appdata v) {
    v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    float h = v.uv.y; float3 p = v.vertex.xyz;
    float band = WindBand(p.xz);
    float2 d = normalize(_WindDir.xy);
    float sway = sin(_Time.y * 1.7 + p.x * 1.3 + p.z * 0.9) * 0.05;
    p.xz += d * (_Bend * (0.2 + band) + sway) * h * h;
    p.y -= _Bend * 0.3 * band * h * h;
    o.pos = UnityObjectToClipPos(float4(p, 1));
    o.world = mul(unity_ObjectToWorld, float4(p, 1)).xyz;
    o.color = v.color * _Tint; o.hb = float2(h, band);
    return o;
   }
   fixed4 frag(v2f i) : SV_Target {
    float h = i.hb.x;
    float3 c = i.color.rgb * (1 + 0.35 * i.hb.y * h);
    c += _Glow.rgb * FieldBacklight(i.world) * h * 0.35;
    return fixed4(FieldFade(c, i.world), 1);
   }
   ENDCG
  }
 }
}
