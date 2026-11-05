Shader "Shootemup/Void Falls"
{
    Properties
    {
        _Clock("Flow time",Float)=0 _Seed("Flow seed",Float)=0 _Curtain("Liquid curtain",Float)=0
        _Edge("Inner shore",Float)=3.4 _Flow("Fall speed",Float)=1 _Shade("Depth shade",Float)=1
    }
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
            float _Clock,_Seed,_Curtain,_Edge,_Flow,_Shade;
            CBUFFER_END
            struct I {float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
            struct O {float4 positionCS:SV_POSITION;float2 p:TEXCOORD0;float2 uv:TEXCOORD1;};
            O Vert(I i)
            {
                O o;float3 p=i.positionOS.xyz;
                if(_Curtain>.5)
                {
                    float u=i.uv.x,t=_Clock*_Flow,phase=_Seed+i.uv.y*2.73;
                    float wave=p.y*.42+t*.65+phase;
                    float shore=_Edge+.34*sin(wave)+.18*sin(wave*2.1+1.2);
                    float fold=.20*sin(p.y*.67+t*1.2+u*4+phase)*(1-u);
                    p.x=(i.uv.y<.5?-1:1)*(shore+u*6+fold);
                    p.z=.32*sin(u*8+p.y*.3+t*.4)+.18*cos(u*17-p.y*.4-t*.5);
                }
                o.positionCS=TransformObjectToHClip(p);o.p=p.xy;o.uv=i.uv;return o;
            }
            float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float N(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(a),Hash(a+float2(1,0)),f.x),lerp(Hash(a+float2(0,1)),Hash(a+1),f.x),f.y);}
            half4 Frag(O i):SV_Target
            {
                float2 p=i.p;
                float3 dark;
                if(_Curtain>.5)
                {
                    float u=i.uv.x*6,v=p.y+_Clock*_Flow*1.55,phase=_Seed+i.uv.y*2.73;
                    // Broad wet folds break into rivulets; the banks themselves move,
                    // so the falling liquid has a silhouette as well as a sheen.
                    float warp=N(float2(u*1.2+phase,v*.19))*2.1;
                    float a=u*3.4+sin(v*.38+phase)*1.15+warp;
                    float folds=.60*sin(a)+.24*sin(a*2.1+v*.23);
                    float dx=1.35*cos(a)+.62*cos(a*2.1+v*.23);
                    float dy=.24*cos(v*.38+phase)*cos(a)+.12*cos(a*2.1+v*.23);
                    float3 normal=normalize(float3(dx,dy,-1.3));
                    float3 lamp=normalize(float3(-.55,.6,-1));
                    float light=saturate(dot(normal,lamp));
                    float gloss=pow(saturate(dot(reflect(-lamp,normal),float3(0,0,-1))),20);
                    float ripple=sin(v*2.7+warp*3+u*1.4)*.5+.5;
                    float lip=exp(-i.uv.x*58)*( .58+.42*sin(v*.65+phase)*sin(v*.65+phase));
                    dark=(float3(.008,.007,.016)+float3(.078,.058,.11)*light*(.65+folds*.25)
                        +float3(.24,.19,.30)*gloss*(.55+ripple*.45)+float3(.14,.10,.20)*lip)*_Shade;
                }
                else
                {
                    float far=N(p*.12+float2(_Seed,_Clock*.012));
                    dark=lerp(float3(.002,.002,.006),float3(.024,.012,.043),far*far);
                    float haze=exp(-dot(p*float2(.24,.13),p*float2(.24,.13)))*.025;
                    dark+=float3(.43,.20,.69)*haze;
                }
                return half4(dark,1);
            }
            ENDHLSL
        }
    }
}
