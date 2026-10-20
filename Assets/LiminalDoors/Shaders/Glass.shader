Shader "LiminalDoors/Glass" {
 // Claude, 2026-10-19. Clear or frosted glass for the Doorspaces (conservatory, concourse, office partitions): a tint you
 // see through, brighter and more opaque at a glancing angle, a soft sky-coloured sheen and an optional distance haze.
 // Transparent, no grab pass, so it is cheap in VR. _Frost raises the opacity for frosted panels.
 Properties {
  _Tint("Tint", Color) = (0.75,0.95,1,1)
  _Opacity("Opacity facing you", Range(0,1)) = 0.12
  _Frost("Frost", Range(0,1)) = 0
  [HDR] _Edge("Glancing sheen", Color) = (1.1,1.2,1.3,1)
  _HazeColor("Haze colour", Color) = (0.9,0.9,0.95,1)
  _HazeStart("Haze start (m)", Float) = 0
  _HazeEnd("Haze full (m, 0 = off)", Float) = 0
 }
 SubShader {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
  Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_instancing
   #include "UnityCG.cginc"
   float4 _Tint, _Edge, _HazeColor; float _Opacity, _Frost, _HazeStart, _HazeEnd;
   struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
   struct v2f { float4 pos:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
   v2f vert(appdata v) {
    v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos = UnityObjectToClipPos(v.vertex); o.world = mul(unity_ObjectToWorld, v.vertex).xyz; o.normal = UnityObjectToWorldNormal(v.normal);
    return o;
   }
   half4 frag(v2f i) : SV_Target {
    float3 view = normalize(_WorldSpaceCameraPos - i.world);
    float fres = pow(1 - abs(dot(view, normalize(i.normal))), 3);
    float3 c = lerp(_Tint.rgb, _Edge.rgb, fres * 0.7);
    float a = saturate(_Opacity + _Frost * 0.65 + fres * 0.45);
    if (_HazeEnd > _HazeStart) {
     float f = saturate((distance(i.world, _WorldSpaceCameraPos) - _HazeStart) / (_HazeEnd - _HazeStart));
     f = f * (2 - f) * 0.92; c = lerp(c, _HazeColor.rgb, f); a = lerp(a, 1, f * 0.6);
    }
    return half4(c, a);
   }
   ENDCG
  }
 }
}
