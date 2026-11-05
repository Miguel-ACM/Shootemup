Shader "Shootemup/Void Rift"
{
    Properties { _Open("Opening",Range(0,1))=0 _Clock("Clock",Float)=0 _Arrival("Arrival",Float)=0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _Open,_Clock,_Arrival;
            CBUFFER_END
            struct I{float4 p:POSITION;float2 uv:TEXCOORD0;};
            struct O{float4 p:SV_POSITION;float2 xy:TEXCOORD0;};
            O Vert(I i){O o;o.p=TransformObjectToHClip(i.p.xyz);o.xy=i.uv*2-1;return o;}
            half4 Frag(O i):SV_Target
            {
                float2 p=i.xy;float y=abs(p.y)/max(.02,_Open);
                float lip=.40*_Open*pow(saturate(1-y*y),1.5);
                float warp=sin(p.y*12-_Clock*7)*.012*_Open;
                float d=abs(p.x+warp)-lip;
                float cap=1-smoothstep(.8,1,y);
                float rim=exp(-abs(d)*115)*cap;
                float aura=exp(-abs(d)*14)*cap*.55;
                float inside=(1-smoothstep(-.01,.015,d))*cap;
                float streak=pow(saturate(.5+.5*cos(atan2(p.y,p.x)*14+_Clock*2)),22);
                streak*=exp(-length(p)*3)*saturate(d*8)*_Open;
                float flow=.55+.45*sin(p.y*25-_Clock*13);
                float3 color=float3(.025,.005,.065)*inside+float3(.48,.10,.95)*(aura+streak*.7);
                color+=float3(.87,.55,1)*rim*(.9+.25*flow)+float3(.18,.04,.34)*aura*_Arrival;
                float alpha=saturate(inside+rim+aura+streak)*smoothstep(0,.13,_Open);
                return half4(color,alpha);
            }
            ENDHLSL
        }
    }
}
