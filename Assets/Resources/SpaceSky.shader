Shader "Shootemup/Soft Orbital Sky"
{
    Properties { _Drift("Stellar drift",Vector)=(0,0,0,0) _Seed("Sky seed",Float)=0 }
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
            float4 _Drift;float _Seed;
            CBUFFER_END
            struct Input { float4 positionOS:POSITION; };
            struct Output { float4 positionCS:SV_POSITION;float2 p:TEXCOORD0; };
            Output Vert(Input v){Output o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.p=v.positionOS.xy;return o;}
            float Hash(float2 p){float3 q=frac(float3(p.xyx)*.1031+_Seed*.013);q+=dot(q,q.yzx+33.33);return frac((q.x+q.y)*q.z);}
            float Stars(float2 p,float scale,float drift)
            {
                p=(p+_Drift.xy*drift)*scale;float2 cell=floor(p),local=frac(p);
                float r=Hash(cell);float2 starCentre=float2(Hash(cell+3.7),Hash(cell+21.1))*.72+.14;
                float d=length(local-starCentre);float radius=lerp(.018,.044,step(.96,r));
                return smoothstep(radius,0,d)*step(.72,r)*lerp(.18,.72,r);
            }
            float3 Planet(float2 p,float2 centre,float radius,float3 tint,float rotation,out float cover)
            {
                float2 q=(p-centre)/radius;float d=length(q);cover=smoothstep(1.015,.955,d);
                float z=sqrt(saturate(1-dot(q,q)));float light=saturate(dot(float3(q.x,q.y,z),normalize(float3(-.5,.65,.75))));
                float bands=.93+.07*sin(q.y*19+sin(q.x*4+rotation));
                float limb=pow(saturate(1-z),3)*.11;
                return tint*(.10+light*.78)*bands+float3(.07,.13,.20)*limb;
            }
            half4 Frag(Output i):SV_Target
            {
                float2 p=i.p;float haze=pow(saturate(.48+.18*sin(p.x*.18+p.y*.07)+.15*cos(p.y*.12-p.x*.14)),3);
                float3 color=lerp(float3(.012,.017,.040),float3(.045,.040,.10),haze);
                float stars=Stars(p,1.5,1)+Stars(p+17,2.6,.54)*.57+Stars(p-11,4.5,.28)*.34;
                color+=stars*float3(.82,.88,1);
                float cover;float2 shift=_Drift.xy*.13;
                float3 blue=Planet(p+shift,float2(-4.9,6.2),3.5,float3(.18,.28,.48),0,cover);color=lerp(color,blue,cover*.88);
                float2 ring=(p+shift-float2(5.5,-6.8));ring=float2(ring.x+.42*ring.y,ring.y*2.6);
                float halo=smoothstep(.11,0,abs(length(ring)-4.9));color=lerp(color,float3(.17,.14,.18),halo*.34);
                float3 lavender=Planet(p+shift,float2(5.5,-6.8),3.75,float3(.32,.22,.29),2,cover);color=lerp(color,lavender,cover*.92);
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
