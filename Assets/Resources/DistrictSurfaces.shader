Shader "Shootemup/District Surfaces"
{
    Properties
    {
        _Grain("Rolled metal, silt and plaster",2D)="gray" {}
        _Finish("Stone, enamel or sugar field",Float)=0
        _SectionOrigin("Scroll coordinate",Float)=0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_Grain); SAMPLER(sampler_Grain);
            CBUFFER_START(UnityPerMaterial)
            float _Finish, _SectionOrigin;
            CBUFFER_END
            struct Input { float4 p:POSITION; half4 c:COLOR; float4 rotor:TEXCOORD1; };
            struct Output { float4 p:SV_POSITION; half4 c:COLOR; float3 local:TEXCOORD0; };
            Output Vert(Input i)
            {
                Output o;
                if (i.rotor.z>.01 && abs(i.rotor.w)>.001)
                {
                    float2 q=i.p.xy-i.rotor.xy;
                    q.y*=i.rotor.z;
                    float sn,cs;
                    sincos(_Time.y*i.rotor.w,sn,cs);
                    q=float2(cs*q.x-sn*q.y,sn*q.x+cs*q.y);
                    i.p.xy=i.rotor.xy+float2(q.x,q.y/i.rotor.z);
                }
                o.p=TransformObjectToHClip(i.p.xyz);
                o.local=i.p.xyz+float3(0,_SectionOrigin,0);
                o.c=i.c;
                return o;
            }
            half4 Frag(Output i):SV_Target
            {
                float2 p=i.local.xy;
                half3 surface=SAMPLE_TEXTURE2D(_Grain,sampler_Grain,p*float2(.34,.25)).rgb;
                float bay=p.y*.31415927;
                half3 c=i.c.rgb;
                if(_Finish<.5)
                {
                    float damp=.75+.36*surface.r+.12*surface.b;
                    float light=pow(saturate(.54+.46*cos(bay*2+p.x*.53)),3);
                    c*=damp*(.74+.24*light);
                    c+=half3(.09,.052,.014)*light*smoothstep(.30,.64,c.r);
                    c=lerp(c,c*half3(.72,.87,.77),smoothstep(.50,.8,surface.b)*.86);
                }
                else if(_Finish<1.5)
                {
                    float skylight=.5+.5*cos(bay*2+p.x*.49);
                    float edge=smoothstep(2.6,4.10,abs(p.x));
                    float wear=(surface.g-.50)*.18+(surface.b-.5)*.11;
                    c*=.78+.12*skylight+.07*surface.r-.11*edge+wear;
                    c+=half3(.005,.012,.022)*(1-skylight);
                }
                else
                {
                    float meadow=smoothstep(-.04,.02,i.local.z);
                    float orchard=.5+.5*cos(p.x*2.6+sin(bay*4)*1.3);
                    c*=.91+.16*surface.r+meadow*(.085*orchard-.038);
                    c+=half3(.030,.013,.024)*(surface.b-.34);
                }
                return half4(c,i.c.a);
            }
            ENDHLSL
        }
    }
}
