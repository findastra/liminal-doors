Shader "LiminalDoors/Pool Tile" {
 // Claude, 2026-10-19. Tiles for Pool After Closing (stop 03): square tiles with grout, each tile a touch different,
 // soft light from the hall's lamps, a wet sheen on floors, and caustic light that wanders across anything under the
 // water (pool floor and walls) or near it (the deck edge and, reflected up, the ceiling over the pool).
 // World-space tiles, so separate boxes line up. Unlit underneath: no scene lights or baking needed.
 Properties {
  _Color("Tile colour", Color) = (0.95,0.74,0.62,1)
  _Color2("Second tile colour", Color) = (0.90,0.66,0.56,1)
  _Grout("Grout colour", Color) = (0.62,0.46,0.42,1)
  _TileSize("Tile size (m)", Float) = 0.2
  _GroutWidth("Grout width (fraction)", Range(0,0.3)) = 0.06
  _LightDir("Lamp light direction", Vector) = (0.3,0.8,-0.4,0)
  _Ambient("Shadow side brightness", Range(0,1)) = 0.6
  [HDR] _Warm("Warm lamp glow", Color) = (0.25,0.12,0.06,1)
  _WaterLevel("Water level (world y)", Float) = -0.15
  _Underwater("Underwater colour", Color) = (0.25,0.85,0.92,1)
  [HDR] _CausticColor("Caustic light", Color) = (1.2,1.6,1.6,1)
  _CausticAbove("Caustics above the water (0 = none)", Range(0,1)) = 0
  _Sheen("Wet sheen", Range(0,1)) = 0
  [HDR] _SheenColor("Sheen colour", Color) = (1.2,0.7,0.9,1)
 }
 SubShader {
  Tags { "RenderType"="Opaque" "Queue"="Geometry" }
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_instancing
   #include "UnityCG.cginc"
   #include "LiminalPool.cginc"
   float4 _Color, _Color2, _Grout, _LightDir, _Warm, _Underwater, _CausticColor, _SheenColor;
   float _TileSize, _GroutWidth, _Ambient, _WaterLevel, _CausticAbove, _Sheen;
   struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
   struct v2f { float4 pos:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
   v2f vert(appdata v) {
    v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
    o.normal = UnityObjectToWorldNormal(v.normal);
    return o;
   }
   fixed4 frag(v2f i) : SV_Target {
    float3 n = normalize(i.normal);
    float2 uv = TileUV(i.world, n) / _TileSize;
    float2 cell = floor(uv), f = frac(uv);
    float2 g = min(f, 1 - f);
    // grout lines anti-aliased by their on-screen size; far away they melt into the tiles' average instead of shimmering
    float aa = max(fwidth(uv.x), fwidth(uv.y));
    float sharp = 1 - smoothstep(_GroutWidth * 0.5 - aa, _GroutWidth + aa, min(g.x, g.y));
    float fade = saturate((aa - 0.12) / 0.25);
    float grout = lerp(sharp, _GroutWidth * 1.6, fade);
    float pick = PoolHash(cell);
    float3 c = lerp(_Color.rgb, _Color2.rgb, pick) * (0.94 + 0.08 * PoolHash(cell + 3.7));
    c *= 0.97 + 0.06 * PoolNoise(uv * 3.3);                  // a little glaze variation inside each tile
    c = lerp(c, _Grout.rgb, grout);
    float d = saturate(dot(n, normalize(_LightDir.xyz)) * 0.5 + 0.5);
    c *= _Ambient + (1 - _Ambient) * d;
    c += _Warm.rgb * saturate(1 - abs(i.world.y - 2.4) / 3.5);   // the lamps' warm band along the walls
    float t = _Time.y;
    float below = smoothstep(_WaterLevel + 0.02, _WaterLevel - 0.06, i.world.y);
    if (below > 0) {
     float depth = saturate((_WaterLevel - i.world.y) / 1.6);
     float3 wet = lerp(c, c * _Underwater.rgb * 1.25, 0.55 + 0.35 * depth);
     float2 cp = abs(n.y) > 0.5 ? i.world.xz : (abs(n.x) > 0.5 ? float2(i.world.z, i.world.y * 0.6) : float2(i.world.x, i.world.y * 0.6));
     wet += _CausticColor.rgb * Caustics(cp, t) * (1 - depth * 0.45) * (1 - grout * 0.6);
     c = lerp(c, wet, below);
    }
    if (_CausticAbove > 0) c += _CausticColor.rgb * 0.35 * Caustics(i.world.xz * 0.6, t * 0.8) * _CausticAbove;
    if (_Sheen > 0) {
     float3 view = normalize(_WorldSpaceCameraPos - i.world);
     float fres = pow(1 - saturate(dot(view, n)), 4);
     float streak = PoolNoise(float2(i.world.x * 0.35, i.world.z * 2.4)) * 0.6 + 0.4;
     c += _SheenColor.rgb * fres * streak * _Sheen * (1 - grout * 0.5);
    }
    return fixed4(c, 1);
   }
   ENDCG
  }
 }
}
