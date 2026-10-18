Shader "LiminalDoors/Soft Glow" {
 // Claude, 2026-10-18. Light that adds to what is behind it: a soft round glow, brightest in the middle. Used for the
 // light standing in a doorway (on a quad) and for door motes (particles; their colour and fade come in as vertex
 // colour). Set the colour above 1 for the bloom to pick it up. Written new for the fresh base.
 Properties {
  [HDR] _Color("Glow colour", Color) = (1,0.6,0.75,1)
  _Softness("Edge softness", Range(0.5,4)) = 1.6
 }
 SubShader {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
  Blend One One ZWrite Off Cull Off
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_instancing
   #include "UnityCG.cginc"
   float4 _Color; float _Softness;
   struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
   struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; UNITY_VERTEX_OUTPUT_STEREO };
   v2f vert(appdata v) {
    v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv * 2 - 1; o.color = v.color; return o;
   }
   half4 frag(v2f i) : SV_Target {
    float a = saturate(1 - length(i.uv));
    a = pow(a * a * (3 - 2 * a), _Softness);
    return half4(_Color.rgb * i.color.rgb * (a * i.color.a), 1);
   }
   ENDCG
  }
 }
}
