Shader "Shootemup/Boss Spell Break"
{
    Properties { _Age("Age",Float)=0 }
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
            CBUFFER_END
            struct I { float4 p:POSITION;float2 uv:TEXCOORD0; };
            struct O { float4 p:SV_POSITION;float2 xy:TEXCOORD0; };
            O Vert(I i) { O o;o.p=TransformObjectToHClip(i.p.xyz);o.xy=(i.uv-.5)*48;return o; }
            float Ring(float r,float t,float delay,float width)
            {
                float a=max(0,t-delay),front=1+24*(1-exp(-a*2.9));
                return exp(-pow((r-front)/width,2))*saturate(a*24)*pow(saturate(1-a/1.15),1.4);
            }
            half4 Frag(O i):SV_Target
            {
                float t=_Age,r=length(i.xy),a=atan2(i.xy.y,i.xy.x);
                float ignition=exp(-t*17)*exp(-r*r/72);
                float pressure=Ring(r,t,0,.46)+Ring(r,t,.11,.19)*.8+Ring(r,t,.24,.09)*.65;
                float rays=pow(abs(cos(a*6)),36)+pow(abs(cos(a*10+.21)),70)*.5;
                float reach=2+16*(1-exp(-t*6));
                float star=rays*exp(-r/7)*(1-smoothstep(reach-1,reach,r))*exp(-t*4.6);
                float core=exp(-r*r/(.18+5*exp(-t*15)))*exp(-t*7);
                float wash=.72*exp(-t*20)*(1-smoothstep(0,24,r));
                float alpha=saturate(ignition+pressure+star+core+wash)*saturate((1.35-t)*4);
                return half4(lerp(float3(.70,.83,1),float3(1,1,1),saturate(pressure+ignition+core+.55)),alpha);
            }
            ENDHLSL
        }
    }
}
