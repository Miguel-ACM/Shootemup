Shader "Shootemup/Hazard Line"
{
    // Additive light along a line renderer, bright in the middle of its width and soft at the edges.
    // Used for lightning: no dark rims, so a bolt reads as light rather than a tube.
    Properties { }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Blend One One
            ColorMask RGB
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Input { float4 positionOS:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Output { float4 positionCS:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            Output Vert(Input i)
            {
                Output o;
                o.positionCS=TransformObjectToHClip(i.positionOS.xyz);
                o.color=i.color;
                o.uv=i.uv;
                return o;
            }
            half4 Frag(Output i):SV_Target
            {
                float across=abs(i.uv.y*2-1);
                float light=saturate(1-across*across)*(.6+.4*saturate(1-across*3));
                return half4(i.color.rgb*i.color.a*light,1);
            }
            ENDHLSL
        }
    }
}
