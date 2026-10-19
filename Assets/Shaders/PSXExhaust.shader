Shader "Shootemup/PSX Exhaust"
{
    Properties { _Tint("Tint", Color) = (1,0.65,0.25,1) _ZTest("Depth test", Float) = 4 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Blend SrcAlpha One
            ZWrite Off
            ZTest [_ZTest]
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _Tint;
            CBUFFER_END
            struct Input { float4 positionOS:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Output { float4 positionCS:SV_POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; };
            Output Vert(Input i)
            {
                Output o;
                o.positionCS=TransformObjectToHClip(i.positionOS.xyz);
                o.color=i.color*_Tint;
                o.uv=i.uv;
                return o;
            }
            half4 Frag(Output i):SV_Target { return i.color; }
            ENDHLSL
        }
    }
}
