Shader "Shootemup/Sculpted Heaven"
{
 Properties { _Tint("Tint",Color)=(1,1,1,1) _HitFlash("Hit",Range(0,1))=0 }
 SubShader { Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
 Pass { Tags { "LightMode"="Universal2D" } Cull Off ZWrite On
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float4 _Tint;float _HitFlash;
 CBUFFER_END
 struct I {float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;};
 struct O {float4 vertex:SV_POSITION;float3 normal:TEXCOORD0;float2 uv:TEXCOORD1;float3 p:TEXCOORD2;};
 O Vert(I i){O o;o.vertex=TransformObjectToHClip(i.vertex.xyz);o.normal=TransformObjectToWorldNormal(i.normal);o.uv=i.uv;o.p=i.vertex.xyz;return o;}
 half4 Frag(O i):SV_Target {
 float tile=floor(i.uv.x*4)+floor(i.uv.y*4)*4;
 float3 n=normalize(i.normal),l=normalize(float3(-.45,.7,-1));
 float diffuse=saturate(dot(n,l));float rim=pow(1-abs(n.z),3);
 float gold=(tile==2||tile==3||tile==7||tile==8)?1:0;
 float3 base=gold?float3(.76,.47,.105):float3(.84,.88,.92);
 if(tile==7)base=float3(.32,.18,.045);
 if(tile==11||tile==4||tile==6)base=float3(.025,.055,.09);
 if(tile==9)base=float3(.14,.48,.62);
 float spec=pow(saturate(dot(reflect(-l,n),float3(0,0,-1))),gold>0?42:72);
 float grain=sin(i.p.x*91+i.p.y*13)*sin(i.p.y*97+i.p.z*19)*.012;
 float3 color=base*(.38+.65*diffuse+grain)+spec*(gold>0?.55:.32)+rim*float3(.08,.095,.12);
 if(tile==8)color=float3(1,.80,.35)*(.88+.12*diffuse);
 return half4(lerp(color*_Tint.rgb,float3(1,1,1),saturate(_HitFlash)),1);
 }
 ENDHLSL } }
}
