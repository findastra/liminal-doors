Shader "LiminalDoors/Portal" {
 // Claude, 2026-10-19. The light standing in an open doorway. Astra: "the door portals need to fill up the square, in
 // unity you got that kind of weird" (the first doorway light was a soft oval floating in the frame). This one fills
 // the opening edge to edge: a luminous veil, mostly opaque, with slow swirling light inside, light rising from the
 // threshold and a bright rim where it meets the frame. The quad is exactly the opening; _Size is its width and height
 // in metres so the rim is the same thickness on every side. HDR colours above 1 are what the bloom picks up.
 Properties {
  [HDR] _Color("Veil colour", Color) = (1.4,0.7,1.0,1)
  [HDR] _Deep("Swirl colour", Color) = (0.8,0.35,0.9,1)
  [HDR] _Rim("Rim colour", Color) = (2.2,1.5,1.9,1)
  _Size("Opening size (m)", Vector) = (0.9,2.24,0,0)
  _Opacity("Opacity", Range(0,1)) = 0.86
  _Speed("Swirl speed", Float) = 0.35
 }
 SubShader {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
  Blend One OneMinusSrcAlpha ZWrite Off Cull Off
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_instancing
   #include "UnityCG.cginc"
   float4 _Color, _Deep, _Rim, _Size; float _Opacity, _Speed;
   struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
   struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
   float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
   float Noise(float2 p) {
    float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f);
    return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x), lerp(Hash(i + float2(0, 1)), Hash(i + 1), f.x), f.y);
   }
   v2f vert(appdata v) {
    v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o;
   }
   half4 frag(v2f i) : SV_Target {
    float2 m = i.uv * _Size.xy;                                  // metres across the opening
    float edge = min(min(m.x, _Size.x - m.x), min(m.y, _Size.y - m.y));
    float t = _Time.y * _Speed;
    // a slow swirl: noise turned by noise, so it drifts like light through water rather than scrolling
    float2 q = m * 1.6;
    float2 w = float2(Noise(q + float2(t, -t * 0.7)), Noise(q * 1.3 + float2(-t * 0.6, t)));
    float swirl = Noise(q * 2.1 + w * 2.4 + float2(0, -t * 1.4)) * 0.65 + Noise(q * 4.3 - w * 1.7) * 0.35;
    // light rising from the threshold in soft vertical threads
    float threads = Noise(float2(m.x * 9.0, m.y * 0.9 - t * 2.2)) * smoothstep(1.6, 0.0, m.y);
    float3 c = lerp(_Deep.rgb, _Color.rgb, smoothstep(0.25, 0.85, swirl));
    c += _Color.rgb * threads * 0.35;
    c *= 0.85 + 0.25 * smoothstep(0.0, 0.6, m.y) * (1 - smoothstep(1.6, 2.4, m.y));
    float rim = exp(-edge / 0.035) + 0.35 * exp(-edge / 0.12);
    c += _Rim.rgb * rim;
    float a = saturate(_Opacity + rim * 0.5);
    return half4(c * a, a);
   }
   ENDCG
  }
 }
}
