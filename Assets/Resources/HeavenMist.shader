Shader "Shootemup/Heaven Mist"
{
    Properties { _Distance("Atmospheric distance",Range(0,1))=0 }
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
            float _Distance;
            CBUFFER_END
            struct I{float4 positionOS:POSITION;float3 normalOS:NORMAL;};
            struct O{float4 positionCS:SV_POSITION;float3 n:TEXCOORD0;float3 p:TEXCOORD1;};
            O Vert(I i){O o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.n=TransformObjectToWorldNormal(i.normalOS);o.p=i.positionOS.xyz;return o;}
            half4 Frag(O i):SV_Target
            {
                float3 n=normalize(i.n);
                float sun=saturate(dot(n,normalize(float3(-.45,.75,-1)))*.5+.5);
                float underside=smoothstep(-1.35,-.25,i.p.y);
                float silver=pow(saturate(1-abs(n.z)),3)*sun;
                float contact=(1-smoothstep(.7,2.0,abs(i.p.x)))*(1-smoothstep(.08,.62,abs(i.p.y+.13)));
                float3 shade=lerp(float3(.51,.64,.77),float3(1,.975,.90),sun*.76+underside*.17);
                shade-=float3(.10,.085,.06)*contact;
                shade+=float3(.075,.065,.045)*silver;
                shade=lerp(shade,float3(.69,.77,.83),_Distance*.58);
                return half4(shade,1);
            }
            ENDHLSL
        }
    }
}
