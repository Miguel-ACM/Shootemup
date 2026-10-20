// Unlit, always-on-top marker for the player's hitbox (the cockpit core and its ring).
Shader "Shootemup/PSXv2 Hitbox"
{
    Properties
    {
        _Color ("Colour", Color) = (1,1,1,1)
        _Tint ("Visibility tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Overlay" }
        Pass
        {
            Name "PSXv2Hitbox"
            Tags { "LightMode"="Universal2D" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _Color, _Tint;
            CBUFFER_END
            float4 Vert(float4 positionOS : POSITION) : SV_POSITION { return TransformObjectToHClip(positionOS.xyz); }
            half4 Frag() : SV_Target { return _Color * _Tint; }
            ENDHLSL
        }
    }
}
