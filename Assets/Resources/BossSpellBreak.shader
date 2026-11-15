Shader "Shootemup/Boss Spell Break"
{
    // Implosion, then rupture. For the first .3 s the arena dims and a ring collapses into the boss;
    // then a brief wash, three pressure fronts (the last in the district colour), rotating rays and
    // a starburst spread outward.
    Properties { _Age("Age",Float)=0 _Accent("District colour",Color)=(1,1,1,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off ZWrite Off ZTest Always
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _Age;
            float4 _Accent;
            CBUFFER_END
            static const float Implode=.3;
            struct I { float4 p:POSITION;float2 uv:TEXCOORD0; };
            struct O { float4 p:SV_POSITION;float2 xy:TEXCOORD0; };
            O Vert(I i) { O o;o.p=TransformObjectToHClip(i.p.xyz);o.xy=(i.uv-.5)*48;return o; }
            float Ring(float r,float t,float delay,float width,float life)
            {
                float a=max(0,t-delay),front=1+30*(1-exp(-a*2.4));
                return exp(-pow((r-front)/width,2))*saturate(a*24)*pow(saturate(1-a/life),1.3);
            }
            half4 Frag(O i):SV_Target
            {
                float t=_Age,r=length(i.xy),a=atan2(i.xy.y,i.xy.x);
                if(t<Implode)
                {
                    // Anticipation: the arena darkens while a bright ring collapses onto the boss.
                    float k=t/Implode;
                    float front=18*pow(1-k,1.5);
                    float ring=exp(-pow((r-front)/(.5+1.2*(1-k)),2))*k;
                    float gather=exp(-r*r/(.4+3*k))*k*k;
                    float dim=.22*smoothstep(0,1,k)*saturate(r/3);
                    float light=saturate(ring+gather);
                    float3 colour=lerp(float3(.70,.83,1),float3(1,1,1),light);
                    colour=lerp(_Accent.rgb,colour,.55+.45*light);
                    float alpha=saturate(max(dim,light));
                    return half4(lerp(float3(.01,.01,.04),colour,light/max(alpha,.001)),alpha);
                }
                float e=t-Implode;
                float ignition=exp(-e*12)*exp(-r*r/140);
                float wash=.5*exp(-e*16);
                float white=Ring(r,e,0,.55,1.3)+Ring(r,e,.1,.2,1.2)*.75;
                float tinted=Ring(r,e,.28,.38,1.2)*.65;
                float spin=a+e*.9;
                float rays=pow(abs(cos(spin*6)),30)+pow(abs(cos(spin*10+.21)),60)*.6+pow(abs(cos(-a*4+e*1.4)),80)*.5;
                float reach=2+18*(1-exp(-e*5));
                float star=rays*exp(-r/7.5)*(1-smoothstep(reach-1.5,reach,r))*exp(-e*4.2);
                float core=exp(-r*r/(.25+7*exp(-e*12)))*exp(-e*5);
                float bright=saturate(ignition+white+star+core+wash);
                float alpha=saturate(bright+tinted)*saturate((1.5-e)*3);
                float3 colour=lerp(float3(.72,.85,1),float3(1,1,1),saturate(white+ignition+core+wash+.5));
                colour=lerp(colour,_Accent.rgb*1.15,saturate(tinted/(bright+tinted+.001)));
                return half4(colour,alpha);
            }
            ENDHLSL
        }
    }
}
