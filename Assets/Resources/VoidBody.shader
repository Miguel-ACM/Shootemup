Shader "Shootemup/Void Body"
{
    Properties { _Tint("Tint",Color)=(1,1,1,1) _Hit("Hit",Range(0,1))=0 }
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
            float4 _Tint;float _Hit;
            CBUFFER_END
            struct I{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
            struct O{float4 positionCS:SV_POSITION;float3 n:TEXCOORD0;float2 uv:TEXCOORD1;float3 p:TEXCOORD2;};
            O Vert(I i){O o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.n=TransformObjectToWorldNormal(i.normalOS);o.uv=i.uv;o.p=i.positionOS.xyz;return o;}
            half4 Frag(O i):SV_Target
            {
                float tile=floor(i.uv.x*4)+4*floor(i.uv.y*4);
                float light=saturate(dot(normalize(i.n),normalize(float3(-.4,.65,-1))));
                float rim=pow(1-abs(normalize(i.n).z),2);
                float eye=(abs(tile-8)<.1||abs(tile-9)<.1)?1:0;
                float vein=abs(tile-3)<.1?1:0;
                float pulse=.76+.24*sin(_Time.y*2.4+i.p.y*3);
                float3 c=lerp(float3(.013,.009,.024),float3(.16,.09,.23),light)+rim*float3(.16,.06,.28);
                c=lerp(c,float3(.72,.25,1)*pulse,vein*.65);
                c=lerp(c,float3(.90,.56,1)*(.8+.2*light),eye);
                return half4(lerp(c*_Tint.rgb,1,saturate(_Hit)),1);
            }
            ENDHLSL
        }
    }
}
