Shader "Shootemup/Scenery Glow"
{
    // Additive light baked into scenery meshes: lamp pools, lanterns, campfires, fireflies and
    // glints on water. Each quad carries its own phase and behaviour, so a whole section of
    // lights animates in one draw without per-light objects or scripts.
    Properties
    {
        _MainTex("Soft light falloff",2D)="white"{}
        _Strength("Fade",Range(0,1))=1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Blend One One
            ColorMask RGB
            ZWrite Off ZTest LEqual Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float _Strength;
            CBUFFER_END
            // behaviour.x: phase. behaviour.y: 0 steady lamp, 1 firefly, 2 fire, 3 water glint.
            struct Input { float4 positionOS:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float2 behaviour:TEXCOORD1; };
            struct Output { float4 positionCS:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            Output Vert(Input i)
            {
                Output o;
                float t=_Time.y;
                float phase=i.behaviour.x;
                float mode=i.behaviour.y;
                float3 p=i.positionOS.xyz;
                float level=1;
                if(mode<.5)
                    level=.92+.08*sin(t*7.3+phase*11)*sin(t*2.1+phase);
                else if(mode<1.5)
                {
                    // Fireflies wander a little and blink slowly, never in unison.
                    p.xy+=float2(sin(t*.61+phase*3.7)+.5*sin(t*1.37+phase),cos(t*.47+phase*2.3)+.5*sin(t*1.11+phase*1.7))*.16;
                    level=pow(saturate(sin(t*1.3+phase*6.28)),3);
                }
                else if(mode<2.5)
                    level=.72+.18*sin(t*9.7+phase)+.10*sin(t*23.1+phase*2.3);
                else
                    level=pow(saturate(sin(t*2.7+phase*6.28)),5);
                o.positionCS=TransformObjectToHClip(p);
                o.color=i.color*level*_Strength;
                o.uv=i.uv;
                return o;
            }
            half4 Frag(Output i):SV_Target
            {
                float falloff=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).r;
                return half4(i.color.rgb*i.color.a*falloff,1);
            }
            ENDHLSL
        }
    }
}
