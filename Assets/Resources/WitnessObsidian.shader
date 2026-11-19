Shader "Shootemup/Witness obsidian"
{
    Properties { _HitFlash("Hit",Float)=0 _Tint("Tint",Color)=(1,1,1,1) }
    SubShader { Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
    Pass { Tags { "LightMode"="Universal2D" } Cull Off ZWrite On
    HLSLPROGRAM
    #pragma vertex vert
    #pragma fragment frag
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    float _HitFlash;float4 _Tint;
    struct V {float4 p:SV_POSITION;float3 n:TEXCOORD0;float4 pigment:COLOR;};
    V vert(float4 p:POSITION,float3 n:NORMAL,float4 c:COLOR){V o;o.p=TransformObjectToHClip(p.xyz);o.n=TransformObjectToWorldNormal(n);o.pigment=c;return o;}
    half4 frag(V i):SV_Target {
        float3 n=normalize(i.n);float face=abs(dot(n,normalize(float3(-.52,.72,-.68))));
        float polish=pow(abs(dot(n,normalize(float3(.26,.54,-.8)))),19);
        float3 c=i.pigment.rgb*(.48+.52*face)+float3(.14,.17,.21)*polish;
        return half4(lerp(c*_Tint.rgb,float3(.88,.85,1),saturate(_HitFlash)),1);
    }
    ENDHLSL } }
}
