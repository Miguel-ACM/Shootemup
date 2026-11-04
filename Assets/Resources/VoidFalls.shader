Shader "Shootemup/Void Falls"
{
    Properties { _Clock("Flow time",Float)=0 _Seed("Flow seed",Float)=0 _Curtain("Liquid curtain",Float)=0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            ZWrite On Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _Clock,_Seed,_Curtain;
            CBUFFER_END
            struct I {float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
            struct O {float4 positionCS:SV_POSITION;float2 p:TEXCOORD0;float2 uv:TEXCOORD1;};
            O Vert(I i) {O o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.p=i.positionOS.xy;o.uv=i.uv;return o;}
            float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float N(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(a),Hash(a+float2(1,0)),f.x),lerp(Hash(a+float2(0,1)),Hash(a+1),f.x),f.y);}
            half4 Frag(O i):SV_Target
            {
                float2 p=i.p;
                float far=N(p*.12+float2(_Seed,_Clock*.012));
                float3 dark=lerp(float3(.002,.002,.006),float3(.024,.012,.043),far*far);
                if(_Curtain>.5)
                {
                    // Two flowing phases blend before either wraps: no reset seams or baked runtime noise.
                    float t=_Clock*.10, a=frac(t),b=frac(t+.5);
                    float2 flow=float2(p.x*2.9+_Seed,p.y*.30);
                    float warp=sin(p.y*.62+_Clock*.21)*.20;
                    float n=lerp(N(flow+float2(warp,a*8)),N(flow+float2(warp,b*8)),abs(a*2-1));
                    float ribs=sin(p.x*13+n*5+sin(p.y*.27+_Clock*.5));
                    float silk=pow(saturate(ribs),16);
                    float sheen=pow(saturate(n*.7+.3*sin(p.y*1.8+_Clock*2.1)),7);
                    float lip=pow(saturate(1-i.uv.x),12);
                    dark+=float3(.09,.055,.16)*silk+float3(.19,.12,.26)*sheen+float3(.17,.09,.27)*lip;
                }
                else
                {
                    float haze=exp(-dot(p*float2(.24,.13),p*float2(.24,.13)))*.025;
                    dark+=float3(.43,.20,.69)*haze;
                }
                return half4(dark,1);
            }
            ENDHLSL
        }
    }
}
