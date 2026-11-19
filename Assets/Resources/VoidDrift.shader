Shader "Shootemup/Void Drift"
{
    Properties { _Clock("Fall time",Float)=0 _Liquid("Liquid beads",Float)=0 _RouteTint("Route light",Color)=(.73,.31,1,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Cull Back ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _Clock,_Liquid;
            float4 _RouteTint;
            CBUFFER_END
            struct I { float4 p:POSITION;float3 n:NORMAL;float4 fall:TEXCOORD0;float4 spin:TEXCOORD1;float4 color:COLOR; };
            struct O { float4 p:SV_POSITION;float3 n:TEXCOORD0;float shade:TEXCOORD1; };
            float3 Turn(float3 p,float3 angle)
            {
                float3 s=sin(angle),c=cos(angle);
                p.yz=float2(p.y*c.x-p.z*s.x,p.y*s.x+p.z*c.x);
                p.xz=float2(p.x*c.y+p.z*s.y,-p.x*s.y+p.z*c.y);
                p.xy=float2(p.x*c.z-p.y*s.z,p.x*s.z+p.y*c.z);return p;
            }
            O Vert(I i)
            {
                O o;float3 angle=i.spin.xyz*_Clock+i.fall.w*float3(.73,1.17,.51);
                if(_Liquid>.5)angle=float3(0,0,sin(_Clock*.5+i.fall.w)*.1);
                float3 p=Turn(i.p.xyz,angle);
                float drift=sin(_Clock*.24+i.fall.w)*(_Liquid>.5?.12:.28);
                p+=float3(i.fall.x+drift,frac((i.fall.y+16-_Clock*i.fall.z)/32)*32-16,i.spin.w);
                o.p=TransformObjectToHClip(p);o.n=TransformObjectToWorldNormal(Turn(i.n,angle));o.shade=i.color.r;return o;
            }
            half4 Frag(O i):SV_Target
            {
                float3 normal=normalize(i.n),lamp=normalize(float3(-.5,.7,-1));
                float light=saturate(dot(normal,lamp));
                float sheen=pow(saturate(dot(reflect(-lamp,normal),float3(0,0,-1))),_Liquid>.5?22:20);
                float body=lerp(.028+.16*light,.012+.083*light,_Liquid);
                float gloss=lerp(.14,.39,_Liquid)*sheen;
                float3 surface=lerp(_RouteTint.rgb,1,.12)*body+lerp(_RouteTint.rgb,1,.38)*gloss;
                return half4(surface*i.shade,1);
            }
            ENDHLSL
        }
    }
}
