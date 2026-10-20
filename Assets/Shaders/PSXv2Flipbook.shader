// Pixel flipbook particles (Texture Sheet Animation supplies the frame UVs).
// Point-sampled, 1-bit alpha cut-out, multiplied by the particle colour; alpha fades
// become ordered dither so effects keep the hard PS1 edge instead of soft blending.
Shader "Shootemup/PSXv2 Flipbook"
{
    Properties
    {
        _MainTex ("Flipbook", 2D) = "white" {}
        [Toggle] _Additive ("Additive", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Name "PSXv2Flipbook"
            Tags { "LightMode"="Universal2D" }
            Blend One OneMinusSrcAlpha
            ZWrite Off
            // Effects sort by layer above the raised hull geometry instead of depth-testing into it.
            ZTest Always
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _Additive;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            Varyings Vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.color = i.color;
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
                uint2 p = (uint2)i.positionCS.xy & 3;
                const float bayer[16] = {0,8,2,10,12,4,14,6,3,11,1,9,15,7,13,5};
                clip(c.a - (bayer[p.y * 4 + p.x] + 0.5) / 16.0);
                c.rgb = floor(saturate(c.rgb) * 31.0 + 0.5) / 31.0;
                // Premultiplied: opaque cut-out, or pure add when _Additive is on.
                return half4(c.rgb, 1 - _Additive);
            }
            ENDHLSL
        }
    }
}
