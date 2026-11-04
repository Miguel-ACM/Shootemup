Shader "Shootemup/Heaven Mist"
{
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
            struct I{float4 positionOS:POSITION;float3 normalOS:NORMAL;};
            struct O{float4 positionCS:SV_POSITION;float3 n:TEXCOORD0;float3 p:TEXCOORD1;};
            O Vert(I i){O o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.n=TransformObjectToWorldNormal(i.normalOS);o.p=i.positionOS.xyz;return o;}
            half4 Frag(O i):SV_Target
            {
                float3 n=normalize(i.n);
                float sun=saturate(dot(n,normalize(float3(-.3,.6,-1)))*.5+.5);
                float scallop=.5+.5*sin(i.p.x*5.1+sin(i.p.y*4.7+i.p.z)*1.6);
                float3 shade=lerp(float3(.55,.69,.81),float3(1,.99,.955),sun);
                shade+=scallop*.025;
                return half4(shade,1);
            }
            ENDHLSL
        }
    }
}
