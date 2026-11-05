Shader "Shootemup/Heaven Ruins"
{
    Properties { _Distance("Atmospheric distance",Range(0,1))=0 _Clock("Wind time",Float)=0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Cull Off ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _Distance,_Clock;
            CBUFFER_END
            struct I { float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0; };
            struct O { float4 p:SV_POSITION;float3 n:TEXCOORD0;float3 local:TEXCOORD1;float tile:TEXCOORD2; };
            O Vert(I i)
            {
                O o;float tile=floor(i.uv.x*4)+floor(i.uv.y*4)*4;
                if(tile==12) i.p.z+=sin(_Clock*1.8+i.p.x*4+i.p.y*3)*.065*saturate((1.64-i.p.y)*1.3);
                o.p=TransformObjectToHClip(i.p.xyz);o.n=TransformObjectToWorldNormal(i.n);o.local=i.p.xyz;o.tile=tile;return o;
            }
            half4 Frag(O i):SV_Target
            {
                float3 n=normalize(i.n), light=normalize(float3(-.5,.8,-1));
                float diffuse=saturate(dot(n,light));
                float gold=(i.tile==3||i.tile==7)?1:0;
                float3 stone=i.tile==1?float3(.64,.68,.70):float3(.90,.89,.82);
                float vein=sin(i.local.x*12+i.local.y*9+sin(i.local.y*7)*1.7)*.5+.5;
                stone-=float3(.045,.035,.018)*pow(vein,18);
                float3 base=gold?float3(.75,.51,.19):stone;
                if(i.tile==7)base=float3(.43,.30,.15);
                if(i.tile==12)base=float3(.84,.79,.65);
                float spec=pow(saturate(dot(reflect(-light,n),float3(0,0,-1))),gold>0?24:42);
                float3 color=base*(.58+.40*diffuse)+spec*(gold>0?.19:.04);
                color=lerp(color,float3(.64,.72,.79),.12+_Distance*.62);
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
