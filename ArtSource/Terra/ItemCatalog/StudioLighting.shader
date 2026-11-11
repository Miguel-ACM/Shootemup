Shader "Shootemup Review/Item Studio"
{
    Properties
    {
        _MainTex ("Palette atlas", 2D) = "white" {}
        _Normals ("Show face normals", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float _Normals;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 n = normalize(input.normalWS);
                if (_Normals > .5)
                    return half4(n * .5 + .5, 1);

                // Wide, soft-box directions give the flat facets distinct values.
                float3 key = normalize(float3(-.65, .81, -.70));
                float3 fill = normalize(float3(.67, -.19, -.57));
                float3 rim = normalize(float3(.30, .73, .62));
                float keyLight = saturate(dot(n, key));
                float fillLight = saturate(dot(n, fill));
                float rimLight = saturate(dot(n, rim));
                float2 swatch = (floor(input.uv * 4) + .5) * .25;
                float3 paint = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, swatch).rgb;
                float3 trim = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).rgb;
                float3 base = lerp(paint, trim, .20);
                float3 illumination = float3(.57, .60, .66)
                    + keyLight * float3(.54, .49, .42)
                    + fillLight * float3(.17, .22, .30)
                    + rimLight * float3(.22, .25, .28);
                float specular = pow(saturate(dot(n, normalize(key + float3(0, 0, -1)))), 15);
                float3 color = base * illumination + specular * float3(.045, .040, .034);
                return half4(saturate(color), 1);
            }
            ENDHLSL
        }
    }
}
