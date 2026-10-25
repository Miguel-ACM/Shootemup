Shader "Shootemup/Caldera molten bed"
{
    Properties { _Flow("Flow",Float)=0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Background" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Cull Off ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _Flow;
            CBUFFER_END
            struct Input {float4 p:POSITION;};
            struct Output {float4 p:SV_POSITION;float2 world:TEXCOORD0;};
            Output Vert(Input i){Output o;o.p=TransformObjectToHClip(i.p.xyz);o.world=i.p.xy;return o;}
            half4 Frag(Output i):SV_Target
            {
                float2 p=i.world+float2(0,_Flow*3.7);
                float bend=sin(p.y*.23+p.x*.16)*.62+sin(p.y*.61-p.x*.20)*.21;
                float ribbons=sin(p.x*1.9+bend+p.y*.35-_Flow*.36);
                float eddies=sin(p.y*.91+sin(p.x*.73)+ribbons*.48);
                float heat=saturate(.46+ribbons*.16+eddies*.12);
                float seams=pow(saturate(.5+.5*sin(p.x*4.7+p.y*.7+bend*2.5)),10)*.10;
                float3 lava=lerp(float3(.24,.061,.014),float3(.57,.23,.043),heat)+float3(.13,.069,.013)*seams;
                return half4(lava,1);
            }
            ENDHLSL
        }
    }
}
