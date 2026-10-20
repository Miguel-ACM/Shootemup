// PSX-style explosion sphere: a low-poly sphere skinned with a scrolling fire texture,
// additively blended (PS1 semi-transparency B+F), vertex-snapped, and dissolved with
// ordered dither as it burns out. PSXFireballs drives _Color/_Fade per sphere.
Shader "Shootemup/PSXv2 Fireball"
{
    Properties
    {
        _MainTex ("Fire (tileable, Point, Repeat)", 2D) = "white" {}
        _Color ("Colour", Color) = (1,1,1,1)
        _Fade ("Burn-out 0..1", Range(0,1)) = 0
        _Scroll ("Texture scroll", Vector) = (0.35,0.9,0,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Name "PSXv2Fireball"
            Tags { "LightMode"="Universal2D" }
            Blend One One
            ZWrite Off
            ZTest Always
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _PSXSnapResolution;
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST, _Color, _Scroll;
                float _Fade;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            Varyings Vert(Attributes i)
            {
                Varyings o;
                float4 cs = TransformObjectToHClip(i.positionOS.xyz);
                float2 res = max((_PSXSnapResolution.x > 0 ? _PSXSnapResolution.xy : float2(569, 320)) * 0.5, 1.0);
                cs.xy = floor(cs.xy / cs.w * res + 0.5) / res * cs.w;
                o.positionCS = cs;
                o.uv = TRANSFORM_TEX(i.uv, _MainTex) + _Scroll.xy * _Time.y;
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                half4 fire = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                uint2 p = (uint2)i.positionCS.xy & 3;
                const float bayer[16] = {0,8,2,10,12,4,14,6,3,11,1,9,15,7,13,5};
                float threshold = (bayer[p.y * 4 + p.x] + 0.5) / 16.0;
                // Darker texels dissolve first, so the sphere breaks up into flame tongues.
                float heat = dot(fire.rgb, float3(0.5, 0.4, 0.1));
                clip(heat + 0.35 - _Fade * 1.35 - threshold * 0.35);
                float3 c = fire.rgb * _Color.rgb;
                c = floor(saturate(c) * 31.0 + 0.5) / 31.0;
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
