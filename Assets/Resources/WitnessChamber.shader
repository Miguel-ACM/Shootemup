Shader "Shootemup/Silver chamber"
{
    Properties { _Clock("Clock",Float)=0 }
    SubShader { Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
    Pass { Tags { "LightMode"="Universal2D" } Cull Off ZWrite Off
    HLSLPROGRAM
    #pragma vertex vert
    #pragma fragment frag
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    float _Clock;
    struct V {float4 p:SV_POSITION;float2 world:TEXCOORD0;};
    V vert(float4 p:POSITION){ V o;o.p=TransformObjectToHClip(p.xyz);o.world=TransformObjectToWorld(p.xyz).xy;return o; }
    float2 turn(float2 p,float a){float s,c;sincos(a,s,c);return float2(c*p.x-s*p.y,s*p.x+c*p.y);}
    float rule(float distance,float width,float aa){return 1-smoothstep(width,width+aa,distance);}
    half4 frag(V i):SV_Target {
        float3 ray=float3(i.world*.13,1);
        ray.xy=turn(ray.xy,.16+_Clock*.048);
        ray.yz=turn(ray.yz,.30+.11*sin(_Clock*.085));
        ray.xz=turn(ray.xz,.58+_Clock*.069);
        float3 a=abs(ray); float span=max(a.x,max(a.y,a.z));
        float3 hit=ray/span;
        float side; float2 uv;
        if(a.x>=a.y && a.x>=a.z){uv=hit.zy;side=.77+.06*sign(hit.x);}
        else if(a.y>=a.z){uv=hit.xz;side=.83+.07*sign(hit.y);}
        else {uv=hit.xy;side=.93;}
        uv=uv*.5+.5;
        float2 edge=min(uv,1-uv);
        float border=min(edge.x,edge.y), aa=max(.0009,fwidth(border));
        float2 tile=abs(frac(uv*4+.5)-.5)*.25;
        float joint=min(tile.x,tile.y);
        float pearl=.31+side*(.69+.024*sin(dot(uv,float2(3.4,5.3))));
        float3 porcelain=float3(.959,.974,.985)*pearl;
        float steel=.40+.32*pow(.5+.5*sin((uv.x+uv.y)*15+_Clock*.5),3);
        porcelain=lerp(porcelain,float3(.71,.75,.78),rule(joint,.0014,aa)*.40);
        porcelain=lerp(porcelain,steel*float3(.92,.97,1),rule(border,.013,aa));
        porcelain=lerp(porcelain,float3(.91,.93,.94),rule(abs(border-.008),.0018,aa));
        porcelain*=1-.18*exp(-border*46);
        float bracket=abs(edge.x+edge.y-.095);
        porcelain=lerp(porcelain,steel.xxx,rule(bracket,.002,aa)*step(max(edge.x,edge.y),.078));
        float pin=length(edge-float2(.030,.030));porcelain*=1-.38*rule(pin,.005,aa);
        return half4(porcelain,1);
    }
    ENDHLSL } }
}
