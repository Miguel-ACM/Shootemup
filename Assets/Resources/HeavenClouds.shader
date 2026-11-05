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
                float lighting=saturate(.61+(relief.g-.5)*.85+(relief.b-.5)*1.15);
                float crest=smoothstep(.40,.77,relief.r);
                float3 shadow=lerp(float3(.38,.50,.66),float3(.52,.63,.76),nearLayer);
                return lerp(shadow,float3(.98,.95,.85),saturate(lighting*.68+crest*.38))+relief.a*.018;
            }
            half4 Frag(O i):SV_Target
            {
                float2 p=i.xy*.027+float2(_Seed,_Clock*.009);
                float4 far=SAMPLE_TEXTURE2D(_CloudAtlas,sampler_CloudAtlas,p);
                float4 near=SAMPLE_TEXTURE2D(_CloudAtlas,sampler_CloudAtlas,p*1.73+float2(.31+_Clock*.0017,.43+_Clock*.005));
                float central=1-smoothstep(1.2,6,abs(i.xy.x));
                float3 color=lerp(float3(.29,.43,.62),Cotton(far,0),smoothstep(.30,.59,far.r));
                float mask=smoothstep(.38,.64,near.r)*(1-central*.38);
                color=lerp(color,Cotton(near,1),mask);
                float shaft=pow(saturate(.5+.5*sin(i.xy.x*.67+i.xy.y*.19)),26);
                color+=float3(.11,.09,.045)*shaft*.42;
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
