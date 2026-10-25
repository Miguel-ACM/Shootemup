Shader "Shootemup/PSX Industrial Ship"
{
    Properties
    {
        _MainTex ("Pixel atlas", 2D) = "white" {}
        _Tint ("Damage flash tint", Color) = (1,1,1,1)
        _HitFlash ("Local hit flash", Range(0,1)) = 0
        [HideInInspector] _PlayerDepth ("Player foreground depth", Float) = 0
        _CockpitBlue ("Blue player cockpit", Float) = 0
        _VertexSnap ("Vertex snap amount", Range(0,1)) = 0.35
        _SnapResolution ("Virtual screen resolution", Vector) = (320,240,0,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "PSXShip"
            Tags { "LightMode"="Universal2D" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Tint;
                float4 _SnapResolution;
                float _VertexSnap;
                float _CockpitBlue;
                float _PlayerDepth;
                float _HitFlash;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float light : TEXCOORD1;
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                float2 screen = output.positionCS.xy / output.positionCS.w;
                float2 resolution = max(_SnapResolution.xy * 0.5, 1.0);
                float2 snapped = floor(screen * resolution + 0.5) / resolution;
                output.positionCS.xy = lerp(screen, snapped, _VertexSnap) * output.positionCS.w;
                // Keep the legacy player in the foreground while preserving its own surface depth.
                if (_PlayerDepth > 0.5)
                    output.positionCS.z = lerp(UNITY_NEAR_CLIP_VALUE * output.positionCS.w, output.positionCS.z, 0.001);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                float3 normal = TransformObjectToWorldNormal(input.normalOS);
                float diffuse = saturate(dot(normal, normalize(float3(-0.45, 0.65, -0.8))));
                output.light = 0.50 + floor(diffuse * 4.0 + 0.5) / 4.0 * 0.60;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Tint;
                clip(color.a - 0.01);
                color.rgb *= input.light;
                color.rgb=lerp(color.rgb,half3(1,.88,.63),saturate(_HitFlash)*.88);
                if(_CockpitBlue>0.5 && input.uv.x<0.25 && input.uv.y>=0.25 && input.uv.y<0.5)
                    color.rgb=lerp(float3(0.01,0.38,1.0),float3(0.36,0.93,1.0),saturate(color.r*2));
                // Five bits per channel evokes the original console's colour precision.
                color.rgb = floor(saturate(color.rgb) * 31.0 + 0.5) / 31.0;
                return color;
            }
            ENDHLSL
        }
    }
}
