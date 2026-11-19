Shader "Shootemup/Witness penumbra"
{
    Properties
    {
        _Silhouette ("Silhouette", 2D) = "black" {}
        _MaskFrame ("Mask frame", Vector) = (0,0,1,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Cull Off ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask RGB
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_Silhouette);
            SAMPLER(sampler_Silhouette);
            float4 _Silhouette_TexelSize;
            struct Varyings { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert(float4 position : POSITION)
            {
                Varyings o;
                o.position = TransformObjectToHClip(position.xyz);
                o.uv = position.xy + .5;
                return o;
            }
            float mask(float2 uv)
            {
                return SAMPLE_TEXTURE2D_LOD(_Silhouette,sampler_Silhouette,uv,1).r;
            }
            half4 frag(Varyings i) : SV_Target
            {
                float2 dx = float2(_Silhouette_TexelSize.x * 2.4, 0);
                float2 dy = float2(0, abs(_Silhouette_TexelSize.y) * 2.4);
                float centre = mask(i.uv);
                float edges = mask(i.uv+dx) + mask(i.uv-dx) + mask(i.uv+dy) + mask(i.uv-dy);
                float corners = mask(i.uv+dx+dy) + mask(i.uv+dx-dy) + mask(i.uv-dx+dy) + mask(i.uv-dx-dy);
                return half4(0,0,0,(4*centre + 2*edges + corners) * (.78/16));
            }
            ENDHLSL
        }
        // Called explicitly on the posed sculpture to fill the small coverage texture.
        Pass
        {
            Name "Silhouette"
            Tags { "LightMode"="WitnessMask" }
            Cull Off ZWrite Off ZTest Always
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 _MaskFrame;
            float4 vert(float4 position : POSITION) : SV_POSITION
            {
                float2 p = (position.xy - _MaskFrame.xy) * _MaskFrame.zw;
                #if UNITY_UV_STARTS_AT_TOP
                    p.y = -p.y;
                #endif
                return float4(p, .5, 1);
            }
            half4 frag() : SV_Target { return 1; }
            ENDHLSL
        }
    }
}
