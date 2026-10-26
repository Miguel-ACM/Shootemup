Shader "Shootemup/Depth worlds"
{
    Properties { _Hell("Infernal world",Float)=0 _Clock("Flow",Float)=0 _Seed("Seed",Float)=0 }
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
            float _Hell,_Clock,_Seed;
            CBUFFER_END
            struct Input { float4 p:POSITION; };
            struct Output { float4 p:SV_POSITION;float2 world:TEXCOORD0; };
            Output Vert(Input v){Output o;o.p=TransformObjectToHClip(v.p.xyz);o.world=v.p.xy;return o;}
            float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7))+_Seed)*43758.54);}
            float Noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(a),Hash(a+float2(1,0)),f.x),lerp(Hash(a+float2(0,1)),Hash(a+1),f.x),f.y);}
            half4 Frag(Output i):SV_Target
            {
                float2 p=i.world+float2(0,_Clock*(_Hell>.5 ? 3.7 : 2.6));
                float n=Noise(p*.42),detail=Noise(p*1.8);
                if(_Hell>.5)
                {
                    float river=abs(p.x-2.6*sin(p.y*.12+_Seed*.1)-.65*sin(p.y*.39));
                    float tributary=abs(p.x+3.6*sin(p.y*.14+1.4));
                    float width=.45+Noise(p*.17+5)*.95;
                    float lava=1-smoothstep(width*.62,width,river);
                    lava=max(lava,(1-smoothstep(.18,.55,tributary))*.84);
                    float pools=smoothstep(.67,.79,Noise(p*.20+31));lava=max(lava,pools*.93);
                    float crust=smoothstep(.38,.75,detail)*.33;
                    float flow=.68+.16*sin(p.y*1.8+n*6-_Clock*2.4)+.12*detail;
                    float3 rock=lerp(float3(.035,.025,.029),float3(.105,.066,.055),n)*(.80+detail*.22);
                    float3 fire=lerp(float3(.35,.105,.018),float3(.73,.34,.065),flow)*(1-crust);
                    return half4(lerp(rock,fire,lava*.86),1);
                }
                float seabed=Noise(p*.12+9)*.55+n*.3;
                float depth=saturate((i.world.y+17)/35);
                float3 c=lerp(float3(.012,.065,.12),float3(.065,.24,.29),seabed*.55+depth*.3);
                float2 q=p*1.25+float2(sin(_Clock*.32+p.y*.18),cos(_Clock*.27+p.x*.21))*.4;
                float a=sin(q.x+sin(q.y*.8)),b=sin(q.y*1.1+cos(q.x*.7));
                float d=sin((q.x+q.y)*.85+sin(_Clock*.2)*.25);
                float caustic=exp2(-min(abs(a+b*.55),abs(b+d*.55))*18);
                c+=float3(.025,.075,.075)*caustic*.55;
                float spread=(14-i.world.y)*.15;
                float rays=pow(saturate(cos((i.world.x-spread+sin(_Clock*.16)*.35)*.79)),7);
                rays+=pow(saturate(cos((i.world.x+spread*.55+2.4)*.65)),10)*.6;
                c+=float3(.04,.105,.13)*rays*(.25+depth*.75)*.65;
                return half4(c,1);
            }
            ENDHLSL
        }
    }
}
