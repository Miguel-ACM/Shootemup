Shader "Shootemup/Void Body"
{
    Properties { _MemorySurface("Remembered obsidian",Float)=0 _MemoryPaint("Memory paint", 2D)="white" {} _Painted("Painted silk",Float)=0  _Tint("Tint",Color)=(1,1,1,1) _HitFlash("Hit",Range(0,1))=0 _Rift("Rift",Range(0,1))=0 [HideInInspector] _HullStatus("Hull afflictions",Float)=0 }
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
            #pragma multi_compile_local _ HULL_AFFLICTED
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../Shaders/HullAffliction.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _Tint;float _HitFlash,_Rift,_HullStatus,_Painted,_MemorySurface;
            CBUFFER_END
            float4 _VoidRouteLight;
            TEXTURE2D(_MemoryPaint); SAMPLER(sampler_MemoryPaint);
            struct I{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
            struct O{float4 positionCS:SV_POSITION;float3 n:TEXCOORD0;float2 uv:TEXCOORD1;float3 p:TEXCOORD2;};
            O Vert(I i){O o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.n=TransformObjectToWorldNormal(i.normalOS);o.uv=i.uv;o.p=i.positionOS.xyz;return o;}
            half4 Frag(O i):SV_Target
            {
                float cut=frac(sin(dot(floor(i.p.xy*18),float2(37.1,91.7)))*17341.17);
                clip(cut-_Rift*1.01);
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
                float bevel=.15+.60*light+.25*abs(normal.x);
                c+=_MemorySurface*float3(.32,.18,.43)*bevel*(1-.65*eye);
                if (_Painted>.5)
                {
                    float pigment=dot(SAMPLE_TEXTURE2D(_MemoryPaint,sampler_MemoryPaint,i.uv).rgb,float3(.3,.55,.15));
                    c=lerp(float3(.062,.027,.104),float3(.82,.56,.98),sqrt(saturate(pigment)))*(.58+.42*light)+spec*.12;
                }
                if (_VoidRouteLight.a > .5)
                {
                    float shine = max(c.r,max(c.g,c.b));
                    c = shine * lerp(_VoidRouteLight.rgb,1,saturate(spec*.52 + eye*.19)) + spec*.04;
                }
                #if defined(HULL_AFFLICTED)
                c=HullAffliction(c,HullSurfacePosition(i.p),_HullStatus);
                #endif
                c+=float3(.54,.15,.9)*saturate(1-abs(cut-_Rift)*12)*_Rift*2;
                return half4(lerp(c*_Tint.rgb,1,saturate(_HitFlash)),1);
            }
            ENDHLSL
        }
    }
}

