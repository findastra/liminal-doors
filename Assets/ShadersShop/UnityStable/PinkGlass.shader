// Unity Stable shaders (shaders-shop, Unity Unicorn), Claude, 2026-10-18. Own work for Astra; free to use in her worlds.
// Pink Glass: tinted, see-through glass that shows the sky's reflection, stronger at grazing angles (Fresnel), with a
// bright sun glint and a soft glowing edge. No grab pass (cheap, VRChat-friendly). Both sides render.
Shader "Unity Stable/Pink Glass"
{
    Properties
    {
        _Tint ("Tint (alpha = how see-through)", Color) = (1, 0.72, 0.84, 0.16)
        _Reflection ("Sky reflection", Range(0, 1.5)) = 0.9
        _ReflectionBlur ("Reflection blur", Range(0, 6)) = 0.5
        [HDR] _EdgeGlow ("Edge glow", Color) = (1.2, 0.5, 0.8, 1)
        _EdgePower ("Edge width (higher = thinner)", Range(0.5, 8)) = 2.5
        [HDR] _GlintColor ("Sun glint", Color) = (2.5, 2.3, 2.1, 1)
        _GlintPower ("Glint sharpness", Range(16, 1024)) = 300
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" }
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            float4 _EdgeGlow, _GlintColor; float _Reflection, _ReflectionBlur, _EdgePower, _GlintPower;
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Tint)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos : SV_POSITION; float3 wpos : TEXCOORD0; float3 wn : TEXCOORD1; UNITY_FOG_COORDS(2) UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };

            v2f vert(appdata v)
            {
                v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_TRANSFER_INSTANCE_ID(v, o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex); o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz; o.wn = UnityObjectToWorldNormal(v.normal);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            float4 frag(v2f i, float facing : VFACE) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float4 tint = UNITY_ACCESS_INSTANCED_PROP(Props, _Tint);
                float3 N = normalize(i.wn) * (facing > 0 ? 1 : -1);
                float3 V = normalize(UnityWorldSpaceViewDir(i.wpos)); float3 L = normalize(UnityWorldSpaceLightDir(i.wpos));
                float nv = saturate(dot(N, V));
                float fres = 0.04 + 0.96 * pow(1.0 - nv, 5.0);
                float3 sky = DecodeHDR(UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, reflect(-V, N), _ReflectionBlur), unity_SpecCube0_HDR);
                float3 col = tint.rgb * tint.a * (_LightColor0.rgb * 0.6 + ShadeSH9(float4(N, 1)));    // the tinted body (premultiplied)
                col += sky * _Reflection * fres;                                                        // reflection, stronger at grazing angles
                col += _EdgeGlow.rgb * pow(1.0 - nv, _EdgePower) * 0.35;                                // soft pink edge
                col += _GlintColor.rgb * _LightColor0.rgb * pow(saturate(dot(N, normalize(L + V))), _GlintPower);
                float alpha = saturate(tint.a + fres * _Reflection * 0.6);
                UNITY_APPLY_FOG_COLOR(i.fogCoord, col, float4(0, 0, 0, 0));
                return float4(col, alpha);
            }
            ENDCG
        }
    }
}
