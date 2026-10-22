Shader "LiminalDoors/Pool Water" {
 // Claude, 2026-10-19. The still pool in Pool After Closing (stop 03): clear turquoise you can see the tiles through,
 // slow overlapping ripples, the hall's pink and warm lights picked up at a glancing angle, and small bright glints.
 // A flat quad at the water level; no reflection probe or grab pass, so it is cheap in VR.
 Properties {
  _Shallow("Water colour", Color) = (0.20,0.78,0.86,1)
  [HDR] _Reflect("Glancing reflection", Color) = (1.1,0.55,0.85,1)
  [HDR] _Glint("Glints", Color) = (2.0,1.8,1.9,1)
  _Clarity("See-through (0..1)", Range(0,1)) = 0.6
  _LightDir("Lamp direction", Vector) = (0.2,0.9,-0.3,0)
  _HazeColor("Haze colour", Color) = (0.95,0.72,0.66,1)
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
   #include "LiminalPool.cginc"
   float4 _Shallow, _Reflect, _Glint, _LightDir; float _Clarity;
   struct appdata { float4 vertex:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
   struct v2f { float4 pos:SV_POSITION; float3 world:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
   v2f vert(appdata v) {
    v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos = UnityObjectToClipPos(v.vertex); o.world = mul(unity_ObjectToWorld, v.vertex).xyz; return o;
   }
   float Height(float2 p, float t) {
    return PoolNoise(p * 1.3 + float2(t * 0.25, t * 0.18)) * 0.6 + PoolNoise(p * 3.1 - float2(t * 0.31, -t * 0.22)) * 0.4;
   }
   half4 frag(v2f i) : SV_Target {
    float t = _Time.y; float2 p = i.world.xz; float e = 0.05;
    float hc = Height(p, t);
    float3 n = normalize(float3((hc - Height(p + float2(e, 0), t)) * 0.9, 1, (hc - Height(p + float2(0, e), t)) * 0.9));
    float3 view = normalize(_WorldSpaceCameraPos - i.world);
    float fres = pow(1 - saturate(dot(view, n)), 3);
    float3 c = _Shallow.rgb * (0.85 + 0.3 * hc);
    c = lerp(c, _Reflect.rgb, fres * 0.8);
    float3 hlf = normalize(normalize(_LightDir.xyz) + view);
    c += _Glint.rgb * pow(saturate(dot(n, hlf)), 220) * 0.8;
    c += _Shallow.rgb * Caustics(p * 0.8, t) * 0.18;
    float a = saturate(lerp(1 - _Clarity, 0.95, fres));
    float hz = _HazeEnd > _HazeStart ? saturate((distance(i.world, _WorldSpaceCameraPos) - _HazeStart) / (_HazeEnd - _HazeStart)) : 0;
    return half4(PoolHaze(c, i.world), lerp(a, 1, hz));   // far off the water turns to haze too, not see-through
   }
   ENDCG
  }
 }
}
