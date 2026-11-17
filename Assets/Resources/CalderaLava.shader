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
                // Basalt rafts ride on a continuous copper-orange melt. The broad
                // thermal field opens pools between plates, with narrower seams in cooler rock.
                float thermal = smoothstep(.23,.76,surface.a*.85+heat*.15);
                float rope = surface.g-.5;
                float edge = surface.r+.055*rope;
                float width = lerp(.028,.23,thermal);
                float seam = 1-smoothstep(width*.22,width+.045,edge);
                // Hot open sheets interrupt the network of cooling rafts. Wrinkles
                // survive in the skin of the sheet before the fragmented rind forms.
                float sheet = smoothstep(.43,.86,thermal) * (.75+.24*surface.b);
                float melt = max(seam,sheet);
                float lip = 1-smoothstep(width+.04,width+.16,edge);
                float3 rind = lerp(float3(.072,.048,.060),float3(.24,.115,.095),surface.b);
                rind += float3(.084,.033,.013)*lip;
                rind += float3(.063,.039,.029)*rope;
                float3 ember = lerp(float3(.35,.115,.050),float3(.63,.235,.081),thermal);
                ember += float3(.105,.070,.029)*rope;
                float core = (1-smoothstep(.003,.062,edge))*(.30+.70*thermal);
                return lerp(rind,ember,melt)+float3(.080,.075,.026)*core;
            }
            half4 Frag(Output i):SV_Target
            {
                float2 p=i.world+float2(0,_Flow*3.7);
                float2 uv=float2(p.x*.111+p.y*.015, p.y*.074);
                float4 flow=SAMPLE_TEXTURE2D(_FlowMap,sampler_FlowMap,float2(p.x*.064,p.y*.050));
                float2 velocity=(flow.rg*2-1)*.078;
                // Same dual-phase principle as PSXValleyRiver: periodically reset the
                // distortion under the other phase, without a visible pop or long-run stretch.
                float phase=frac(_Flow/7+flow.b);
                float phaseB=frac(phase+.5),weight=abs(phase*2-1);
                float2 drift=float2(0,_Flow*.019);
                float4 a=SAMPLE_TEXTURE2D(_CrustMap,sampler_CrustMap,uv+drift-velocity*(phase-.5));
                float4 b=SAMPLE_TEXTURE2D(_CrustMap,sampler_CrustMap,uv+drift-velocity*(phaseB-.5));
                // Shade phases separately to retain the narrow hot cracks during blending.
                float heat=flow.a;
                float3 c=lerp(MoltenSurface(a,heat),MoltenSurface(b,heat),weight);
                return half4(c,1);
            }
            ENDHLSL
        }
    }
}
