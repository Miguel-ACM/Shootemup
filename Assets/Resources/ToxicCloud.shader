Shader "Shootemup/Toxic Cloud"
{
    // A pool of poisonous gas (Compost's heap): rolling muted-jade fumes inside a soft, wobbling
    // edge, darker folds and lighter crests, with small bubbles swelling and popping. Object space
    // is the unit quad (-.5..5); _Life fades the cloud in and out and _Seed varies each heap.
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
            float Hash(float2 p) { p=frac(p*float2(123.34,345.45)); p+=dot(p,p+34.345); return frac(p.x*p.y); }
            float Noise(float2 p)
            {
                float2 c=floor(p),f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(Hash(c),Hash(c+float2(1,0)),f.x),lerp(Hash(c+float2(0,1)),Hash(c+1),f.x),f.y);
            }
            float Fbm(float2 p) { return Noise(p)*.5+Noise(p*2.07+1.7)*.28+Noise(p*4.1+4.3)*.14+Noise(p*8.3+2.1)*.08; }
            half4 Frag(O i):SV_Target
            {
                float2 q=i.q; float t=_Clock;
                float2 s=float2(_Seed*7.13,_Seed*3.71);
                float r=length(q);
                float angle=atan2(q.y,q.x);
                // A soft lobed edge that breathes.
                float edge=.78+.12*sin(angle*3+t*.9+_Seed)+.08*sin(angle*5-t*1.3+_Seed*2)+.06*(Fbm(q*2.2+s+t*.25)-.5);
                float body=smoothstep(edge,edge-.2,r);
                // Rolling fumes: two layers drifting against each other and slowly rising.
                float2 drift=float2(t*.12,t*.22);
                float fumes=Fbm(q*1.8+s+drift+Fbm(q*1.2-drift*.7+s)*1.2);
                float folds=smoothstep(.35,.75,fumes);
                // Bubbles: a few cells swell into rings and pop.
                float2 cell=floor(q*3.2+s), f=frac(q*3.2+s)-.5;
                float phase=frac(t*.55+Hash(cell)*7);
                float size=.08+.26*phase;
                float ring=smoothstep(.05,0,abs(length(f-(float2(Hash(cell+3),Hash(cell+9))-.5)*.4)-size))*step(.62,Hash(cell+1))*(1-phase);
                float3 deep=float3(.17,.38,.30), jade=float3(.40,.70,.56), pale=float3(.74,.96,.84), rimTone=float3(.05,.16,.14);
                float3 colour=lerp(deep,jade,folds);
                colour=lerp(colour,pale,smoothstep(.58,.88,fumes)*.75);
                colour=lerp(colour,pale,ring*.9);
                // A darker teal rim outlines the pool so it reads on grass, sand or snow alike.
                float rim=smoothstep(edge-.3,edge-.1,r)*(1-smoothstep(edge-.1,edge-.02,r));
                colour=lerp(colour,rimTone,rim*.55);
                float alpha=body*(.6+.28*folds)+ring*body*.3+rim*.12;
                alpha*=saturate(_Life)*(.45+.55*_Strength);
                return half4(colour*alpha,alpha);
            }
            ENDHLSL
        }
    }
}
