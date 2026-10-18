Shader "LiminalDoors/Field Ground" {
 // Claude, 2026-10-18. The Faraway Field's ground (stop 02): mottled silver-green under the grass, a worn path read from
 // a mask texture, soft slope shading toward the low sun, the wind bands passing over it and haze far off. Unlit.
 Properties {
  _Grass("Ground under the grass", Color) = (0.2,0.29,0.23,1)
  _GrassLight("Ground, lighter patches", Color) = (0.42,0.5,0.4,1)
  _Path("Path", Color) = (0.66,0.54,0.56,1)
  _PathMask("Path mask", 2D) = "black" {}
  _PathRect("Path mask area (x0, z0, size)", Vector) = (-32,-8,64,0)
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
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_instancing
   #include "UnityCG.cginc"
   #include "LiminalField.cginc"
   float4 _Grass, _GrassLight, _Path, _PathRect; sampler2D _PathMask;
   struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
   struct v2f { float4 pos:SV_POSITION; float3 world:TEXCOORD0; float3 opos:TEXCOORD1; float3 normal:TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };
   v2f vert(appdata v) {
    v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
    o.opos = v.vertex.xyz;
    o.normal = UnityObjectToWorldNormal(v.normal);
    return o;
   }
   fixed4 frag(v2f i) : SV_Target {
    float2 p = i.opos.xz;
    float n = FieldNoise(p * 0.15) * 0.6 + FieldNoise(p * 0.7) * 0.4;
    float3 c = lerp(_Grass.rgb, _GrassLight.rgb, n);
    c *= 1 + 0.12 * WindBand(p);
    float m = tex2D(_PathMask, (p - _PathRect.xy) / _PathRect.z).r;
    c = lerp(c, _Path.rgb * (0.85 + 0.3 * FieldNoise(p * 2.3)), m);
    float3 l = normalize(float3(_SunDir.x, 0.6, _SunDir.z));
    c *= 0.8 + 0.3 * saturate(dot(normalize(i.normal), l) * 0.5 + 0.5);
    c += _Glow.rgb * FieldBacklight(i.world) * 0.08;
    return fixed4(FieldFade(c, i.world), 1);
   }
   ENDCG
  }
 }
}
