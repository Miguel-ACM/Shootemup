Shader "Shootemup/Caldera molten bed"
{
    Properties
    {
        _Flow("Flow",Float)=0
        [NoScaleOffset] _CrustMap("Crust distance, folds, grain, heat",2D)="white"{}
        [NoScaleOffset] _FlowMap("Curl velocity and phase",2D)="gray"{}
    }
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
            TEXTURE2D(_CrustMap);SAMPLER(sampler_CrustMap);
            TEXTURE2D(_FlowMap);SAMPLER(sampler_FlowMap);
            CBUFFER_START(UnityPerMaterial)
            float _Flow;
            CBUFFER_END
            struct Input {float4 p:POSITION;};
            struct Output {float4 p:SV_POSITION;float2 world:TEXCOORD0;};
            Output Vert(Input i){Output o;o.p=TransformObjectToHClip(i.p.xyz);o.world=i.p.xy;return o;}
            float3 MoltenSurface(float4 surface,float heat)
            {
                float distance=surface.r-(surface.a-.45)*.32;
                // An orange shoulder under dark cooling rafts, with narrow yellow-hot seams.
                float molten=1-smoothstep(.17,.43,distance+(surface.g-.5)*.09);
                float core=1-smoothstep(.012,.07,distance);
                float folds=smoothstep(.45,.72,surface.g);
                float3 crust=lerp(float3(.085,.037,.022),float3(.23,.075,.030),surface.b);
                float3 magma=lerp(float3(.55,.105,.012),float3(.89,.27,.027),heat);
                magma*=.81+surface.g*.25;
                magma+=float3(.055,.026,.004)*folds;
                float3 c=lerp(crust,magma,molten);
                return lerp(c,float3(.99,.43,.055),core*(.35+.30*heat));
            }
            half4 Frag(Output i):SV_Target
            {
                float2 p=i.world+float2(0,_Flow*3.7);
                float2 uv=p/11;
                float4 flow=SAMPLE_TEXTURE2D(_FlowMap,sampler_FlowMap,p/19);
                float2 velocity=(flow.rg*2-1)*.045;
                // Same dual-phase principle as PSXValleyRiver: periodically reset the
                // distortion under the other phase, without a visible pop or long-run stretch.
                float phase=frac(_Flow/7+flow.b);
                float phaseB=frac(phase+.5),weight=abs(phase*2-1);
                float2 drift=float2(0,_Flow*.019);
                float4 a=SAMPLE_TEXTURE2D(_CrustMap,sampler_CrustMap,uv+drift-velocity*(phase-.5));
                float4 b=SAMPLE_TEXTURE2D(_CrustMap,sampler_CrustMap,uv+drift-velocity*(phaseB-.5));
                // Shade phases separately to retain the narrow hot cracks during blending.
                float heat=.35+flow.a*.65;
                float3 c=lerp(MoltenSurface(a,heat),MoltenSurface(b,heat),weight);
                return half4(c,1);
            }
            ENDHLSL
        }
    }
}
