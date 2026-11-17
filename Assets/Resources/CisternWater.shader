Shader "Shootemup/Cistern Water"
{
    Properties
    {
        _LakeWake("Wake from the cistern", Float) = 0
        [NoScaleOffset] _FlowMap("Current direction and eddies", 2D) = "gray" {}
        [NoScaleOffset] _SurfaceMap("Surface folds and grain", 2D) = "gray" {}
    }
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
            TEXTURE2D(_FlowMap); SAMPLER(sampler_FlowMap);
            TEXTURE2D(_SurfaceMap); SAMPLER(sampler_SurfaceMap);

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
                if (i.color.a < .01)
                    return half4(i.color.rgb, 1);
                float t = _Time.y;
                float2 p = i.world;
                float2 uv = p * float2(.164, .089) + float2(0, t * .044);
                float4 flow = SAMPLE_TEXTURE2D(_FlowMap,sampler_FlowMap,p*.075);
                float2 current = (flow.rg * 2 - 1) * .15;
                float phase = frac(t * .23 + flow.b), halfPhase = frac(phase + .5);
                // A current continually stretches and refreshes two overlapping surface phases.
                float3 a = SAMPLE_TEXTURE2D(_SurfaceMap,sampler_SurfaceMap,uv-current*(phase-.5)).rgb;
                float3 b = SAMPLE_TEXTURE2D(_SurfaceMap,sampler_SurfaceMap,uv-current*(halfPhase-.5)).rgb;
                float3 detail = lerp(a,b,abs(phase*2-1));
                float2 local = i.local * float2(1, 1.13);
                float radius = length(local);
                float fold = sin((p.y + sin(p.x * 1.75 + detail.g * 6) * .43 + detail.r * 1.65) * 10 + t * 1.7);
                float sheen = smoothstep(.58,.96,fold) * smoothstep(.42,.76,detail.g);
                float reflection = smoothstep(.64,.91,detail.b) * (.35 + sheen);
                float wake = smoothstep(.63, .98, sin(radius * 11 - t * 3 + detail.g * 2.2));
                wake *= smoothstep(.95, 1.70, radius) * (1 - smoothstep(3.3, 4.3, radius)) * _LakeWake;
                float bank = smoothstep(3.1, 3.9, abs(i.local.x)) * (1-_LakeWake);
                float foam = smoothstep(.70,.92,detail.g + sheen*.15) * bank;
                float3 color = i.color.rgb * lerp(.79,1.17,detail.g);
                color += float3(.058,.164,.176) * (reflection*.76 + sheen*.65 + wake*.61);
                color += float3(.13,.22,.19) * foam * .59;
                // Ripples start at the two physical outlet lips. Broken reflections pick up the lanterns.
                float side = i.local.x < 0 ? -1 : 1;
                float2 outlet = i.local - float2(side*2.47,side*4.85-.46);
                float dist = length(outlet*float2(1,1.35));
                float ripple = sin(dist*23-t*3.7+detail.g*.95);
                float jet = exp(-dist*1.58)*smoothstep(.26,.93,ripple)*(1-_LakeWake);
                float2 glint = i.local-float2(side*2.86,side*4.85+.89);
                glint.x += (detail.g-.5)*.62+sin(p.y*9+t)*.13;
                float amber = exp(-dot(glint*float2(1.85,.65),glint*float2(1.85,.65)));
                color += float3(.15,.22,.23)*jet + float3(.26,.16,.065)*amber*(.15+.85*sheen)*(1-_LakeWake);
                float shaft=.5+.5*cos(i.local.y*.6283185+i.local.x*.52);
                color *= .90+.16*shaft;
                color += float3(.02,.062,.078)*shaft*(.4+detail.r);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
