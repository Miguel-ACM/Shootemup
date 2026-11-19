Shader "Shootemup/Desert Dust"
{
    Properties { _Clock("Wind time",Float)=0 _Strength("Gust strength",Range(0,1))=0 _Seed("Seed",Float)=0 _Layer("Layer",Float)=0 _Wind("Wind direction",Float)=1 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _Clock,_Strength,_Seed,_Layer,_Wind;
            CBUFFER_END
            struct Input { float4 positionOS:POSITION; };
            struct Output { float4 positionCS:SV_POSITION;float2 p:TEXCOORD0; };
            Output Vert(Input i){Output o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.p=i.positionOS.xy;return o;}
            float Hash(float2 p){p=frac(p*float2(123.34,345.45));p+=dot(p,p+34.345);return frac(p.x*p.y);}
            float Noise(float2 p)
            {
                float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(Hash(a),Hash(a+float2(1,0)),f.x),lerp(Hash(a+float2(0,1)),Hash(a+1),f.x),f.y);
            }
            float Cloud(float2 p){return .58*Noise(p)+.28*Noise(p*2.07+17)+.14*Noise(p*4.13+31);}
            half4 Frag(Output i):SV_Target
            {
                if (_Layer > 1.5)
                {
                    // This plane fits the playfield exactly. Distance runs from zero
                    // at its centre to one at either edge, independent of the HUD.
                    float edge=abs(i.p.x)/15;
                    if (edge < .19) return half4(0,0,0,0);
                    float2 drift=float2(i.p.x*.087-_Wind*_Clock*.32+_Seed,
                                        i.p.y*.12+_Clock*.072);
                    float billow=Cloud(drift);
                    float ribbons=Noise(float2(drift.x*.72,drift.y*5.3+billow*2.8));
                    float windward=-sign(i.p.x)*_Wind;
                    float front=edge+(billow-.5)*.27+(ribbons-.5)*.075+windward*.045;
                    float cover=smoothstep(.33-.045*_Strength,.78,front);
                    float opacity=cover*(.935+.045*billow);
                    float grit=Noise(float2(drift.x*2.1,drift.y*18+ribbons*1.8));
                    float streak=smoothstep(.56,.86,grit)*smoothstep(.32,.73,ribbons);
                    float light=saturate(billow*.72+ribbons*.18+streak*.17+(1-cover)*.16);
                    float3 dust=lerp(float3(.39,.26,.155),float3(.82,.64,.40),light);
                    return half4(dust,opacity);
                }
                float2 wind=float2(i.p.x-_Clock*(2.2+_Layer*1.7)*_Wind,i.p.y+_Clock*.42);
                wind+=float2(_Seed,_Seed*.71);
                float2 p=wind*float2(.18,.35);
                p.y+=.42*sin(p.x*.7+_Clock*.19);
                float n=Cloud(p+float2(0,Cloud(p*.6)*1.3));
                if(_Layer<.5)
                {
                    float density=smoothstep(.28,.69,n);
                    float billow=Cloud(p+float2(-.13,.17));
                    float3 sand=lerp(float3(.53,.38,.22),float3(.85,.69,.43),billow);
                    return half4(sand,density*(.07+.52*_Strength));
                }
                float threads=Cloud(wind*float2(.075,1.8)+float2(0,n*.7));
                float wisps=smoothstep(.46,.76,threads)*smoothstep(.26,.66,n);
                return half4(.88,.74,.49,wisps*(.12+.35*_Strength));
            }
            ENDHLSL
        }
    }
}
