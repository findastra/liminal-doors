Shader "LiminalDoors/Glitter Paint" {
 // Claude, 2026-10-19. Astra: "the door more glittery". Door frames and leaves painted with glitter: a soft-lit colour
 // full of tiny flecks, each a little mirror tilted its own way. A fleck flashes when it lines up between you and the
 // light, so the glitter shifts as you walk past, and each one twinkles on its own time as well. The flecks are HDR, so
 // the bloom turns the brightest into sparkles. Fixed light direction, no scene lights or baking.
 Properties {
  _Color("Paint colour", Color) = (0.98,0.33,0.55,1)
  [HDR] _Emission("Glow", Color) = (0.3,0.03,0.1,1)
  [HDR] _GlitterColor("Glitter colour", Color) = (3,2,2.6,1)
  _Density("Glitter density", Range(0,1)) = 0.55
  _FleckSize("Fleck size (m)", Float) = 0.012
  _Sharpness("Flash sharpness", Range(2,80)) = 18
  _LightDir("Light direction", Vector) = (0.3,0.6,-0.7,0)
  _Ambient("Shadow side brightness", Range(0,1)) = 0.55
 }
 SubShader {
  Tags { "RenderType"="Opaque" "Queue"="Geometry" }
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_instancing
   #include "UnityCG.cginc"
   float4 _Color, _Emission, _GlitterColor, _LightDir; float _Density, _FleckSize, _Sharpness, _Ambient;
   struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
   struct v2f { float4 pos:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
   float3 Hash33(float3 p) {
    p = frac(p * float3(0.1031, 0.1030, 0.0973));
    p += dot(p, p.yxz + 33.33);
    return frac((p.xxy + p.yxx) * p.zyx);
   }
   v2f vert(appdata v) {
    v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
    o.normal = UnityObjectToWorldNormal(v.normal);
    return o;
   }
   fixed4 frag(v2f i) : SV_Target {
    float3 n = normalize(i.normal), l = normalize(_LightDir.xyz), view = normalize(_WorldSpaceCameraPos - i.world);
    float d = saturate(dot(n, l) * 0.5 + 0.5);
    float3 c = _Color.rgb * (_Ambient + (1 - _Ambient) * d) + _Emission.rgb;
    // flecks: one per cell of a fine 3D grid, a small disc around a random point, tilted off the surface normal
    float3 q = i.world / _FleckSize, cell = floor(q);
    float3 h = Hash33(cell);
    if (h.x < _Density) {
     float3 at = cell + 0.2 + 0.6 * Hash33(cell + 19.7);
     float r = length(q - at);
     float disc = saturate(1.6 - r * 2.6);
     float3 tilt = normalize(n + (Hash33(cell + 7.3) - 0.5) * 1.4);
     float flash = pow(saturate(dot(tilt, normalize(l + view))), _Sharpness);
     float tw = 0.35 + 0.65 * saturate(sin(_Time.y * (2.0 + h.y * 5.0) + h.z * 60.0) * 1.5);
     c += _GlitterColor.rgb * disc * (flash * 1.6 + 0.12) * tw;
    }
    return fixed4(c, 1);
   }
   ENDCG
  }
 }
}
