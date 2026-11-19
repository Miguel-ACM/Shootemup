Shader "Shootemup/Fractured tally" {
Properties { [PerRendererData] _MainTex("Tally",2D)="white"{} }
SubShader { Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
Pass { Cull Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
struct V {float4 p:SV_POSITION;float4 c:COLOR;float2 uv:TEXCOORD0;float2 glass:TEXCOORD1;};
sampler2D _MainTex;
V vert(float4 p:POSITION,float4 c:COLOR,float2 uv:TEXCOORD0,float2 mark:TEXCOORD1){V o;o.p=UnityObjectToClipPos(p);o.c=c;o.uv=uv;o.glass=mark;return o;}
half4 frag(V i):SV_Target {
    half4 face=tex2D(_MainTex,i.uv);
    float band=dot(i.uv,float2(.76,1.35))+i.glass.y*1.9;
    float silver=.28+.42*smoothstep(.22,.65,frac(band))+.26*exp(-32*abs(frac(band)-.72));
    face.rgb=lerp(face.rgb,silver*float3(.77,.89,1),i.glass.y);
    return lerp(face,half4(1,1,1,1),i.glass.x)*i.c;
}
ENDHLSL } } }
