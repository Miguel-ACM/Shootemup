Shader "Shootemup/PSX Valley River"
{
    Properties
    {
        _FlowTime("Flow time",Float)=0
        _BridgeY("Bridge station",Float)=4
        _Brightness("Brightness",Float)=1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            ZWrite On Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 _PSXSnapResolution;
            CBUFFER_START(UnityPerMaterial)
            float _FlowTime,_BridgeY,_Brightness;
            CBUFFER_END
            struct Input { float4 positionOS:POSITION;float2 uv:TEXCOORD0; };
            struct Output { float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0; };
            Output Vert(Input i)
            {
                Output o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);
                float2 grid=max(_PSXSnapResolution.xy*.5,1);
                o.positionCS.xy=floor(o.positionCS.xy/o.positionCS.w*grid+.5)/grid*o.positionCS.w;
                o.uv=i.uv;return o;
            }
            float Hash(float2 p)
            {
                // Repeat the noise over the complete 96-unit landscape, including recycled sections.
                p.y=p.y-floor(p.y/192)*192;
                return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);
            }
            float Noise(float2 p)
            {
                float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(Hash(a),Hash(a+float2(1,0)),f.x),lerp(Hash(a+float2(0,1)),Hash(a+1),f.x),f.y);
            }
            half4 Frag(Output i):SV_Target
            {
                float u=i.uv.x,y=i.uv.y,edge=abs(u*2-1);
                float t=_FlowTime;
                // Faster centre, slower margins. All features travel down-channel, not across it.
                float speed=.45+.55*(1-edge*edge);
                float along=y+t*speed;
                float bend=sin(y*6.2831853/24+u*4)*.06;
                float broad=Noise(float2(u*5+bend,along*2));
                float fine=Noise(float2(u*23+bend*3,along*2+31));
                float shallows=smoothstep(.48,.99,edge+.10*(broad-.5));
                float3 c=lerp(float3(.105,.225,.228),float3(.235,.32,.255),shallows);
                c+=(broad-.5)*float3(.025,.044,.038);
                // Broken, stretched highlights; restrained enough to remain behind hostile fire.
                float ripple=sin(along*6.2831853*2+sin(u*28+y*6.2831853/12)*1.7);
                float glint=smoothstep(.83,.98,ripple)*smoothstep(.54,.78,fine);
                float thread=smoothstep(.69,.82,fine)*(.24+.76*(1-edge));
                c+=float3(.055,.085,.074)*(glint*.8+thread*.45);
                float bankFoam=smoothstep(.91,.99,edge)*smoothstep(.54,.75,broad)*(.5+.5*fine);
                // Narrow V wakes attached to the two stone cutwaters below the bridge.
                float downstream=frac((_BridgeY-y+96)/96)*96;
                float wakeWidth=.021+downstream*.018;
                float pier=min(abs(u-.13),abs(u-.87));
                float wake=(1-smoothstep(.007,.025,abs(pier-wakeWidth)))*(1-smoothstep(.2,2.7,downstream));
                wake*=smoothstep(.52,.82,Noise(float2(u*31,(y+t*.9)*2)));
                c=lerp(c,float3(.37,.45,.365),saturate(bankFoam*.38+wake*.5));
                // Coarse colour steps are intentional; the normal game compositor supplies PSX dithering.
                c=floor(c*95+.5)/95;
                return half4(c*_Brightness,1);
            }
            ENDHLSL
        }
    }
}
