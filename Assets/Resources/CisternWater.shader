Shader "Shootemup/Cistern Water"
{
    Properties { _LakeWake("Wake from the cistern", Float) = 0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Cull Off ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float _LakeWake;
            CBUFFER_END
            struct Input { float4 vertex : POSITION; float4 color : COLOR; };
            struct Varyings { float4 position : SV_POSITION; float2 local : TEXCOORD0; float2 world : TEXCOORD1; float4 color : COLOR; };

            Varyings Vert(Input i)
            {
                Varyings o;
                o.position = TransformObjectToHClip(i.vertex.xyz);
                o.local = i.vertex.xy;
                o.world = TransformObjectToWorld(i.vertex.xyz).xy;
                o.color = i.color;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float2 p = i.local;
                float t = _Time.y;
                float radius = length(p * float2(1, 1.15));
                float wake = smoothstep(.67, .98, sin(radius * 12 - t * 2.3 + p.x * .34));
                wake *= smoothstep(.85, 1.55, radius) * (1 - smoothstep(3.35, 4.25, radius)) * _LakeWake;
                float2 flow = i.world;
                float surface = sin(flow.y * 13.5 + flow.x * 2.9 + sin(flow.x * 2.1 - t * .39) * .83 + t * 1.1);
                surface = smoothstep(.70, .98, surface) * (.55 + .45 * sin(flow.x * 5.8 - flow.y * 1.74 + t * .41));
                float3 reflection = float3(.054, .095, .082) * (wake * .78 + surface * .32);
                return half4(i.color.rgb + reflection * i.color.a, 1);
            }
            ENDHLSL
        }
    }
}
