Shader "Shootemup/Candy Clouds"
{
    Properties { _Clock("Cloud time",Float)=0 _Seed("Cloud seed",Float)=0 }
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
            float _Clock,_Seed;
            CBUFFER_END
            struct Input { float4 positionOS:POSITION; };
            struct Output {float4 positionCS:SV_POSITION;float2 p:TEXCOORD0;};
            Output Vert(Input i){Output o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.p=i.positionOS.xy;return o;}
            float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float Noise(float2 p)
            {
                float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(Hash(a),Hash(a+float2(1,0)),f.x),lerp(Hash(a+float2(0,1)),Hash(a+1),f.x),f.y);
            }
            float Billow(float2 p){return .7*Noise(p)+.23*Noise(p*2.03+11)+.07*Noise(p*4.1+23);}
            float Puffs(float2 p)
            {
                float2 cell=floor(p),f=frac(p);float d=4;
                for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)
                {
                    float2 o=float2(x,y),key=cell+o;
                    float2 center=.5+.31*float2(sin(Hash(key)*6.28),cos(Hash(key+17)*6.28));
                    float circle=length(f-o-center)-lerp(.37,.66,Hash(key+37));
                    float h=saturate(.5+.5*(circle-d)/.24);
                    d=lerp(circle,d,h)-.24*h*(1-h);
                }
                return d+(Noise(p*3.1)-.5)*.045;
            }
            float3 Cotton(float2 p,float3 shadow,float3 cream,out float alpha)
            {
                float d=Puffs(p);
                float light=saturate(.52+(Puffs(p+float2(-.055,.075))-d)*5.2);
                float height=smoothstep(.14,-.32,d);
                alpha=smoothstep(.20,-.10,d);
                return lerp(shadow,cream,saturate(light*.62+height*.38));
            }
            half4 Frag(Output i):SV_Target
            {
                float2 p=i.p*float2(.22,.18)+float2(_Seed,_Clock*.105);
                float glow=saturate(1-abs(i.p.x)*.07);
                float3 color=lerp(float3(.48,.32,.57),float3(.76,.46,.62),glow);
                float farAlpha,nearAlpha;
                float3 far=Cotton(p*.78+float2(14,_Clock*.023),float3(.52,.36,.60),float3(.80,.59,.76),farAlpha);
                color=lerp(color,far,farAlpha*.78);
                float2 nearP=p*1.27+float2(sin(p.y*.9)*.17+_Clock*.015,9);
                float3 near=Cotton(nearP,float3(.65,.38,.57),float3(.96,.72,.77),nearAlpha);
                float banks=.55+.25*smoothstep(1,6,abs(i.p.x));
                color=lerp(color,near,nearAlpha*banks);
                // A soft peach light catches the cloud tops; lavender remains in the hollows.
                color+=float3(.045,.025,.005)*Billow(p*1.4+31)*glow;
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
