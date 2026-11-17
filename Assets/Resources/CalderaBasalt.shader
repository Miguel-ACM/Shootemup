Shader "Shootemup/Caldera basalt"
{
    Properties { [NoScaleOffset] _StoneMap("Fractures and mineral grain", 2D)="gray" {} }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Cull Off ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_StoneMap); SAMPLER(sampler_StoneMap);
            struct Input {float4 p:POSITION;float4 color:COLOR;};
            struct Output {float4 p:SV_POSITION;float3 local:TEXCOORD0;float4 color:COLOR;};
            Output Vert(Input i){Output o;o.p=TransformObjectToHClip(i.p.xyz);o.local=i.p.xyz;o.color=i.color;return o;}
            half4 Frag(Output i):SV_Target
            {
                float2 p = i.local.xy;
                // Each 20-unit shelf spans whole repeats in both UV coordinates,
                // so a mineral seam continues past the join between scroll tiles.
                float2 uv = float2(p.x*.145+p.y*.05,p.y*.10-p.x*.037);
                float4 stone = SAMPLE_TEXTURE2D(_StoneMap,sampler_StoneMap,uv);
                float joint = 1-smoothstep(.015,.11,stone.r+(stone.g-.5)*.05);
                float bedding = smoothstep(.50,.87,.5+.5*sin(p.x*3.1+p.y*3.14159+stone.g*6));
                float shade = (.86+stone.b*.22+stone.a*.12)*(1-joint*.18);
                float3 rock = i.color.rgb * shade + float3(.012,.009,.014)*bedding;
                return half4(rock,1);
            }
            ENDHLSL
        }
    }
}
