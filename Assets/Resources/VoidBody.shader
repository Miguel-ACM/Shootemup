Shader "Shootemup/Void Body"
{
    Properties { _Tint("Tint",Color)=(1,1,1,1) _HitFlash("Hit",Range(0,1))=0 }
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
            float4 _Tint;float _HitFlash;
            CBUFFER_END
            struct I{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
            struct O{float4 positionCS:SV_POSITION;float3 n:TEXCOORD0;float2 uv:TEXCOORD1;float3 p:TEXCOORD2;};
            O Vert(I i){O o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.n=TransformObjectToWorldNormal(i.normalOS);o.uv=i.uv;o.p=i.positionOS.xyz;return o;}
            half4 Frag(O i):SV_Target
            {
                float tile=floor(i.uv.x*4)+4*floor(i.uv.y*4);
                float3 normal=normalize(i.n),lamp=normalize(float3(-.4,.65,-1));
                float light=saturate(dot(normal,lamp));
                float rim=pow(saturate(1-abs(normal.z)),2.5);
                float eye=(abs(tile-8)<.1||abs(tile-9)<.1)?1:0;
                float vein=abs(tile-3)<.1?1:0;
                float pulse=.76+.24*sin(_Time.y*2.4+i.p.y*3);
                float spec=pow(saturate(dot(reflect(-lamp,normal),float3(0,0,-1))),34);
                float plate=abs(tile-14)<.1?1:0;
                float3 c=lerp(float3(.018,.012,.030),float3(.14,.080,.22),light)*(.65+plate*.55)+rim*float3(.24,.14,.39);
                c+=spec*float3(.30,.22,.43);
                c=lerp(c,float3(.43,.16,.68)*pulse,vein*.72);
                float3 iris=lerp(float3(.35,.09,.54),float3(.88,.58,1),pow(light,.45));
                iris=iris*(.94+.06*sin(_Time.y*1.8+i.p.y*2))+spec*.22;
                c=lerp(c,iris,eye);
                return half4(lerp(c*_Tint.rgb,1,saturate(_HitFlash)),1);
            }
            ENDHLSL
        }
    }
}

