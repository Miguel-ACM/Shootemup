Shader "Shootemup/Toxic Cloud"
{
    // A pool of poisonous gas (Compost's heap), redrawn after Miguel rejected the soft noise
    // smudge (15 November 2026): a cluster of billowing gas puffs, each a cel-shaded ball in
    // three muted-jade bands with a small glint, outlined in dark teal where it overlaps the
    // next. A ring of puffs slowly turns and swells around a central one, small wisps bubble
    // up off the top and shrink away, and a couple of bubbles rise and pop inside. Object space
    // is the unit quad (-.5..5); _Life grows the cloud in and shrinks it out, _Strength thins
    // it as the heap ages and _Seed varies each heap.
    Properties
    {
        _Life("Life 0..1",Range(0,1))=1
        _Strength("Strength",Range(0,1))=1
        _Seed("Seed",Float)=0
        _Clock("Clock",Float)=0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Blend One OneMinusSrcAlpha
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _Life,_Strength,_Seed,_Clock;
            CBUFFER_END
            struct I { float4 p:POSITION; };
            struct O { float4 p:SV_POSITION; float2 q:TEXCOORD0; };
            O Vert(I i) { O o; o.p=TransformObjectToHClip(i.p.xyz); o.q=i.p.xy*2; return o; }
            float Hash(float n) { return frac(sin(n*91.345+_Seed*17.17)*43758.5453); }

            static const float Outline=.03;

            // Keeps the frontmost puff under the pixel: its height, its local position and the
            // distance to its edge.
            void Puff(float2 q, float2 c, float r, float z, inout float best, inout float2 local, inout float edge)
            {
                if (r<=.001) return;
                float2 p=(q-c)/r;
                float l=dot(p,p);
                if (l>=1) return;
                float h=sqrt(1-l)*r+z;
                if (h<=best) return;
                best=h; local=p; edge=(1-sqrt(l))*r;
            }

            half4 Frag(O i):SV_Target
            {
                float2 q=i.q; float t=_Clock;
                float grow=sqrt(saturate(_Life))*(.88+.12*saturate(_Strength));
                float best=-10, edge=0; float2 local=0;
                // The central puff and a ring of six that turns and swells around it.
                Puff(q,float2(.02*sin(t*.8),.04+.03*sin(t*.9+_Seed)),(.34+.03*sin(t*1.1))*grow,.12,best,local,edge);
                float spin=t*.16+_Seed*.7;
                [unroll] for (int k=0;k<6;k++)
                {
                    float a=spin+k*1.0472+.25*sin(t*.5+k*2.3);
                    float d=(.36+.05*sin(t*.7+k*1.9))*grow;
                    float r=(.27+.05*Hash(k)+.035*sin(t*1.2+k*2.1))*grow;
                    Puff(q,float2(cos(a),sin(a)*.86)*d,r,.04*sin(k*2.1+_Seed),best,local,edge);
                }
                // Wisps bubble up off the top, swell, then shrink away as they rise.
                [unroll] for (int w=0;w<3;w++)
                {
                    float phase=frac(t*.32+w/3.0+Hash(w+7)*.2);
                    float x=(Hash(w+11)-.5)*.62+.06*sin(phase*6.28+w);
                    float y=.26+phase*.62;
                    float r=.16*pow(saturate(sin(phase*3.1416)),.7)*grow;
                    Puff(q,float2(x,y*grow),r,.24+phase*.1,best,local,edge);
                }
                if (best<-5) return 0;

                // Cel shading from the top left: shadow, body, light, and a small glint.
                float3 n=float3(local,sqrt(saturate(1-dot(local,local))));
                float lit=dot(n,normalize(float3(-.48,.56,.68)));
                float3 outline=float3(.05,.15,.13), shadow=float3(.17,.38,.31), body=float3(.33,.60,.49), light=float3(.58,.83,.69), glint=float3(.86,.98,.91);
                float3 colour=lit<.38?shadow:lit<.78?body:light;
                colour=lit>.965?glint:colour;

                // Bubbles rise inside the gas and pop at the end of their climb.
                [unroll] for (int b=0;b<2;b++)
                {
                    float phase=frac(t*.55+b*.5+Hash(b+21));
                    float2 c=float2((Hash(b+31)-.5)*.5,-.28+phase*.5);
                    float r=.05+(phase>.88?(phase-.88)*.6:0);
                    float d=length(q-c)-r;
                    float pop=phase>.88?1-(phase-.88)/.12:1;
                    if (d<0) colour=lerp(colour,d>-.018?outline:(length(q-c-float2(-.016,.018))<.016?glint:light),pop);
                }

                colour=edge<Outline?outline:colour;
                float alpha=(edge<Outline?.92:.78)*saturate(_Life*1.6)*(.7+.3*saturate(_Strength));
                return half4(colour*alpha,alpha);
            }
            ENDHLSL
        }
    }
}
