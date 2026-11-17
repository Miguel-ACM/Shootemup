Shader "Shootemup/Dream Canvas"
{
    Properties { _MainTex("Painted procession", 2D) = "white" {} }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Cull Off ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            struct Input { float4 p:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 p:SV_POSITION; float2 uv:TEXCOORD0; };
            Varyings Vert(Input i)
            {
                Varyings o;
                o.p = TransformObjectToHClip(i.p.xyz);
                o.uv = i.uv;
                return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float turn = i.uv.y*6.2831853;
                float drift = sin(turn*8 + _Time.y*.38) + .33*sin(turn*20 - _Time.y*.53);
                float2 uv = i.uv + float2(drift*.004, .0005*sin(i.uv.x*6 + _Time.y*.32));
                half3 paint = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv).rgb;
                float grazing = .86 + .045*cos(turn*11 + i.uv.x*4 + drift);
                return half4(paint*grazing,1);
            }
            ENDHLSL
        }
    }
}
