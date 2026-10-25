Shader "Shootemup/Snow Surface"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            ZWrite On Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Input { float4 positionOS:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR; };
            struct Output { float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR; };
            Output Vert(Input i)
            {
                Output o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;o.color=i.color;return o;
            }
            half4 Frag(Output i):SV_Target
            {
                // Ground-locked powder grain repeats with the eighty-unit landscape.
                float2 cell=floor(i.uv*36);cell.y-=floor(cell.y/2880)*2880;
                float grit=frac(sin(dot(cell,float2(127.1,311.7)))*43758.5453)-.5;
                float edge=saturate((abs(i.uv.x)-2.6)*.27);
                float ripple=sin(i.uv.x*8+sin(i.uv.y*.392699)*2+sin(i.uv.y*.942478))*edge*.006;
                return half4(i.color.rgb+grit*.021+ripple,1);
            }
            ENDHLSL
        }
    }
}
