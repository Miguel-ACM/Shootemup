Shader "Shootemup/PSX Valley"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            ZWrite On Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 _PSXSnapResolution;
            struct Input { float4 positionOS:POSITION; float3 normalOS:NORMAL; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Output { float4 positionCS:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            Output Vert(Input i)
            {
                Output o;
                o.positionCS=TransformObjectToHClip(i.positionOS.xyz);
                float2 grid=max(_PSXSnapResolution.xy*.5,1);
                float2 ndc=o.positionCS.xy/o.positionCS.w;
                o.positionCS.xy=floor(ndc*grid+.5)/grid*o.positionCS.w;
                float3 n=TransformObjectToWorldNormal(i.normalOS);
                float light=.72+.28*floor(saturate(dot(n,normalize(float3(-.4,.65,-.8))))*3+.5)/3;
                o.color=float4(i.color.rgb*light,1);
                o.uv=i.uv;return o;
            }
            half4 Frag(Output i):SV_Target
            {
                // Quiet, low contrast texels; detail comes primarily from the authored shapes.
                float2 p=floor(i.uv*12);
                p.y=p.y-floor(p.y/1152)*1152;
                float grain=frac(sin(dot(p,float2(12.9898,78.233)))*43758.5453);
                float3 c=i.color.rgb*(.985+grain*.03);
                return half4(c,1);
            }
            ENDHLSL
        }
    }
}
