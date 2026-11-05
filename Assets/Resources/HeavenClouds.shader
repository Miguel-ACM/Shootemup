Shader "Shootemup/Heaven Clouds"
{
    Properties { _CloudAtlas("Cloud relief",2D)="gray" {} _Clock("Cloud time",Float)=0 _Seed("Cloud seed",Float)=0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            ZWrite On Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_CloudAtlas);SAMPLER(sampler_CloudAtlas);
            CBUFFER_START(UnityPerMaterial)
            float _Clock,_Seed;
            CBUFFER_END
            struct I{float4 p:POSITION;};
            struct O{float4 p:SV_POSITION;float2 xy:TEXCOORD0;};
            O Vert(I i){O o;o.p=TransformObjectToHClip(i.p.xyz);o.xy=i.p.xy;return o;}
            float3 Cotton(float4 relief,float nearLayer)
            {
                float lighting=saturate(.51+(relief.g-.5)*1.15+(relief.b-.5)*1.35);
                float crest=smoothstep(.43,.79,relief.r);
                float3 shadow=lerp(float3(.44,.59,.73),float3(.56,.68,.79),nearLayer);
                return lerp(shadow,float3(.99,.96,.88),saturate(lighting*.70+crest*.32))+relief.a*.012;
            }
            half4 Frag(O i):SV_Target
            {
                float2 p=i.xy*.039+float2(_Seed,_Clock*.007);
                // Filter the baked relief at cloud scale; tiny normal variations
                // otherwise turn this distant ocean into noisy, sparkling gravel.
                float4 far=SAMPLE_TEXTURE2D_LOD(_CloudAtlas,sampler_CloudAtlas,p,2.7);
                float4 near=SAMPLE_TEXTURE2D_LOD(_CloudAtlas,sampler_CloudAtlas,p*1.83+float2(.31+_Clock*.0011,.43+_Clock*.004),2.2);
                float4 wisps=SAMPLE_TEXTURE2D_LOD(_CloudAtlas,sampler_CloudAtlas,p*3.1+float2(-.27-_Clock*.0014,.18+_Clock*.002),2.5);
                float central=1-smoothstep(1.2,6,abs(i.xy.x));
                float3 color=lerp(float3(.39,.55,.73),Cotton(far,0),smoothstep(.29,.60,far.r));
                float mask=smoothstep(.36,.66,near.r)*(1-central*.48);
                color=lerp(color,Cotton(near,1),mask);
                color=lerp(color,Cotton(wisps,1),smoothstep(.59,.82,wisps.r)*.24*(1-central*.65));
                float shaft=pow(saturate(.5+.5*sin(i.xy.x*.57+i.xy.y*.18+_Seed*.3)),14);
                color+=float3(.085,.071,.040)*shaft*(.35+.25*saturate(i.xy.y*.035+.5));
                color=lerp(color,float3(.60,.71,.81),central*.10);
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
