Shader "LiminalDoors/Soft Lit" {
 // Claude, 2026-10-18. Doors and stones: a colour with soft wrap-around light from one fixed direction plus an optional
 // HDR glow (above 1.1 the Pinkscape bloom picks it up). Unlit underneath, so it needs no scene lights or baking.
 Properties {
  _Color("Colour", Color) = (1,1,1,1)
  [HDR] _Emission("Glow", Color) = (0,0,0,1)
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
   float4 _Color, _Emission, _LightDir; float _Ambient;
   struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
   struct v2f { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
   v2f vert(appdata v) {
    v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos = UnityObjectToClipPos(v.vertex); o.normal = UnityObjectToWorldNormal(v.normal); return o;
   }
   fixed4 frag(v2f i) : SV_Target {
    float d = saturate(dot(normalize(i.normal), normalize(_LightDir.xyz)) * 0.5 + 0.5);
    return fixed4(_Color.rgb * (_Ambient + (1 - _Ambient) * d) + _Emission.rgb, 1);
   }
   ENDCG
  }
 }
}
