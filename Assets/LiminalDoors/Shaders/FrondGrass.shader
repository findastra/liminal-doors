Shader "LiminalDoors/Frond Grass" {
 // Claude, 2026-10-19. The Faraway Field's tall pink grass (stop 02). Astra: "focus on the grass ... make it look like
 // the grass in the Pink Universe world"; earlier, "work on the grass physics and texture. make it taller. make it
 // interesting." Our own take on that look, written new: every tuft is two crossed cards of a leafy frond (the atlas
 // LiminalStop02 draws), deep rose at the root to pale pink-white at the tips.
 //   uv0.x  atlas column (one frond per quarter), uv0.y 0 at the root, 1 at the tip
 //   uv1    the tuft's root (stop-local), so the whole tuft bends as one
 //   colour per-tuft tint (linear); alpha is the tuft's own sway phase
 // Physics: broad wind gusts travel across the field (LiminalField.cginc), each tuft flutters on its own, and tufts bend
 // away from you as you walk through. LiminalGrassPush (U#) sets _UdonLiminalPlayer to the local player's feet each frame
 // (xyz world, w = 1 while it runs); with w = 0 nobody pushes (the editor, renders).
 Properties {
  _MainTex("Frond atlas", 2D) = "white" {}
  _Cutoff("Alpha cutoff", Range(0,1)) = 0.42
  _Root("Root colour", Color) = (0.42,0.10,0.32,1)
  _Mid("Middle colour", Color) = (0.93,0.42,0.80,1)
  _Tip("Tip colour", Color) = (1,0.86,0.97,1)
  _Bend("Bend in a gust (m)", Float) = 0.38
  _Flutter("Flutter (m)", Float) = 0.06
  _PushRadius("Push radius (m)", Float) = 1.1
  _PushStrength("Push strength (m)", Float) = 0.55
  _WindDir("Wind direction (xy)", Vector) = (0.8,0.6,0,0)
  _WindScale("Wind bands per metre", Float) = 0.035
  _WindSpeed("Wind speed", Float) = 1.1
  _SunDir("Sun direction", Vector) = (0.12,0.04,1,0)
  _Glow("Backlight glow", Color) = (1,0.70,0.86,1)
  _FadeColor("Haze colour", Color) = (0.90,0.64,0.84,1)
  _FadeStart("Haze start (m)", Float) = 45
  _FadeEnd("Haze full (m)", Float) = 190
 }
 SubShader {
  Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "IgnoreProjector"="True" }
  Cull Off
  AlphaToMask On
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_instancing
   #include "UnityCG.cginc"
   #include "LiminalField.cginc"
   sampler2D _MainTex; float _Cutoff;
   float4 _Root, _Mid, _Tip; float _Bend, _Flutter, _PushRadius, _PushStrength;
   float4 _UdonLiminalPlayer;   // set by LiminalGrassPush; never declared in Properties so it stays a global
   struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float3 root:TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };
   struct v2f { float4 pos:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float3 world:TEXCOORD1; float3 hbp:TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };
   v2f vert(appdata v) {
    v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    float h = v.uv.y, h2 = h * h; float3 p = v.vertex.xyz;
    float band = WindBand(v.root.xz);
    float2 d = normalize(_WindDir.xy);
    float ph = v.color.a * 6.2832;
    float flutter = sin(_Time.y * 2.3 + ph) * 0.6 + sin(_Time.y * 3.7 + ph * 1.7) * 0.4;
    float2 side = float2(-d.y, d.x);
    p.xz += (d * (_Bend * (0.18 + band)) + (d * 0.6 + side * 0.8) * (_Flutter * flutter)) * h2;
    p.y -= _Bend * 0.28 * band * h2;
    // walking through: tufts lean out of your way and down a little, most at the tips
    float3 rootW = mul(unity_ObjectToWorld, float4(v.root, 1)).xyz;
    float2 away = rootW.xz - _UdonLiminalPlayer.xz;
    float dist = length(away);
    float near = _UdonLiminalPlayer.w * saturate(1 - dist / _PushRadius) * step(abs(rootW.y - _UdonLiminalPlayer.y), 2.5);
    near = near * near * (3 - 2 * near);
    float2 dirW = dist > 0.001 ? away / dist : float2(1, 0);
    float2 dirO = mul((float3x3)unity_WorldToObject, float3(dirW.x, 0, dirW.y)).xz;
    p.xz += dirO * (_PushStrength * near * h2 * 1.4);
    p.y -= _PushStrength * 0.55 * near * h2;
    o.pos = UnityObjectToClipPos(float4(p, 1));
    o.world = mul(unity_ObjectToWorld, float4(p, 1)).xyz;
    o.color = v.color; o.uv = v.uv; o.hbp = float3(h, band, near);
    return o;
   }
   fixed4 frag(v2f i) : SV_Target {
    float4 t = tex2D(_MainTex, i.uv);
    float h = i.hbp.x;
    float3 c = lerp(_Root.rgb, _Mid.rgb, smoothstep(0.0, 0.55, h));
    c = lerp(c, _Tip.rgb, smoothstep(0.5, 1.0, h));
    c *= i.color.rgb * t.rgb;                          // per-tuft tint, leaf shading from the atlas
    c *= 1 + 0.28 * i.hbp.y * h;                       // a gust lifts the pale undersides
    c += _Glow.rgb * FieldBacklight(i.world) * h * 0.45;   // light through the tips toward the low sun
    // alpha to coverage: a crisp edge that the MSAA smooths
    float a = (t.a - _Cutoff) / max(fwidth(t.a), 0.0001) + 0.5;
    clip(a);                                           // without MSAA (editor renders) this is a plain cutout
    return fixed4(FieldFade(c, i.world), saturate(a));
   }
   ENDCG
  }
 }
}
