Shader "Shootemup/Heaven Angel"
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
            struct O{float4 positionCS:SV_POSITION;float3 n:TEXCOORD0;float2 uv:TEXCOORD1;};
            O Vert(I i){O o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.n=TransformObjectToWorldNormal(i.normalOS);o.uv=i.uv;return o;}
            half4 Frag(O i):SV_Target
            {
                float tile=floor(i.uv.x*4)+4*floor(i.uv.y*4);
                float3 n=normalize(i.n),light=normalize(float3(-.45,.7,-1));
                float diffuse=.42+.58*saturate(dot(n,light));
                float spec=pow(saturate(dot(reflect(-light,n),float3(0,0,-1))),28);
                float3 base=float3(.96,.91,.73);
                if(tile==3||tile==2)base=float3(.92,.59,.13);
                if(tile==7)base=float3(.37,.22,.065);
                if(tile==8)base=float3(1,.82,.34);
                if(tile==9)base=float3(.12,.48,.66);
                if(tile==11)base=float3(.014,.022,.035);
                if(tile==15)base=float3(.99,.97,.88);
                float3 color=base*diffuse+spec*(tile==11?.08:.30);
                if(tile==8)color+=base*.22;
                if(tile==15)color+=base*.12;
                return half4(lerp(color*_Tint.rgb,1,saturate(_HitFlash)),1);
            }
            ENDHLSL
        }
    }
}
