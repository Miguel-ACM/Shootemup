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
                    float bend=sin(p.y*.18+_Seed)*.65+sin(p.y*.51)*.22;
                    float fracture=abs(sin(p.x*.74+bend+sin(p.y*.27)*1.2));
                    float lava=pow(saturate(1-fracture*8),2);
                    float heat=.74+.16*sin(_Clock*1.5+p.y*.8)+detail*.15;
                    float3 rock=lerp(float3(.045,.025,.031),float3(.13,.065,.052),n)*(.85+detail*.20);
                    float3 fire=float3(.56,.105,.015)*heat+float3(.18,.06,.008)*pow(lava,4);
                    return half4(rock+fire*lava,1);
                }
                float seabed=Noise(p*.12+9)*.55+n*.3;
                float3 c=lerp(float3(.018,.068,.115),float3(.035,.20,.25),seabed);
                float2 q=p*.65+float2(sin(_Clock*.32+p.y*.18),cos(_Clock*.27+p.x*.21))*.4;
                float caustic=pow(saturate(1-abs(sin(q.x+sin(q.y*1.3))*sin(q.y+sin(q.x*.8)))*12),3);
                c+=float3(.055,.12,.13)*caustic*.38;
                float rays=pow(saturate(sin(i.world.x*.65+i.world.y*.16+sin(_Clock*.17)*.4)),12);
                c+=float3(.045,.085,.10)*rays*saturate((i.world.y+14)/30)*.4;
                return half4(c,1);
            }
            ENDHLSL
        }
    }
}
