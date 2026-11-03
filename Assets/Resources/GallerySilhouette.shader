Shader "Shootemup/Gallery silhouette"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            ZWrite On Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 vert(float4 position:POSITION):SV_POSITION { return TransformObjectToHClip(position.xyz); }
            half4 frag():SV_Target { return half4(.002,.003,.004,1); }
            ENDHLSL
        }
    }
}
