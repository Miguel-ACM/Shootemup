Shader "Shootemup/PSX Aura"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Input { float4 positionOS:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Output { float4 positionCS:SV_POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; };
            Output Vert(Input i){Output o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.color=i.color;o.uv=i.uv;return o;}
            half4 Frag(Output i):SV_Target
            {
                float r=length(i.uv*2-1);
                float haze=exp(-r*r*5)*.32;
                float rim=exp(-pow((r-.62)*11,2))*.22;
                float edge=saturate((1-r)*6);
                return half4(i.color.rgb,i.color.a*(haze+rim)*edge);
            }
            ENDHLSL
        }
    }
}
