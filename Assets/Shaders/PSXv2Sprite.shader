// Flat textured sprite for background dressing (level 1 clouds and their ground shadows):
// point-sampled, tinted, alpha-blended like PS1 semi-transparency, vertex-snapped to the
// low-res grid. _Tint.a scales the texture alpha (e.g. faint black shadows).
Shader "Shootemup/PSXv2 Sprite"
{
    Properties
    {
        _MainTex ("Sprite (Point)", 2D) = "white" {}
        _Tint ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Name "PSXv2Sprite"
            Tags { "LightMode"="Universal2D" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _PSXSnapResolution;
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST, _Tint;
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
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _Tint;
                clip(c.a - 0.01);
                return c;
            }
            ENDHLSL
        }
    }
}
