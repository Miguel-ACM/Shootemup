Shader "Shootemup/PSX Valley Foliage"
{
    Properties
    {
        _MainTex("Leaf cards",2D)="white"{}
        _FlowTime("Wind time",Float)=0
        _Brightness("Brightness",Float)=1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            ZWrite On Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            float4 _PSXSnapResolution;
            CBUFFER_START(UnityPerMaterial)
            float _FlowTime,_Brightness;
            CBUFFER_END
            struct Input { float4 positionOS:POSITION;float2 uv:TEXCOORD0;float2 motion:TEXCOORD1;float4 color:COLOR; };
            struct Output { float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR; };
            Output Vert(Input i)
            {
                Output o;
                i.positionOS.x+=sin(_FlowTime*1.4+i.motion.x)*.022*i.motion.y;
                i.positionOS.y+=cos(_FlowTime*1.1+i.motion.x)*.011*i.motion.y;
                o.positionCS=TransformObjectToHClip(i.positionOS.xyz);
                float2 grid=max(_PSXSnapResolution.xy*.5,1);
                o.positionCS.xy=floor(o.positionCS.xy/o.positionCS.w*grid+.5)/grid*o.positionCS.w;
                o.uv=i.uv;o.color=i.color;return o;
            }
            half4 Frag(Output i):SV_Target
            {
                half4 leaf=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
                clip(leaf.a-.5);
                return half4(leaf.rgb*i.color.rgb*_Brightness,1);
            }
            ENDHLSL
        }
    }
}
