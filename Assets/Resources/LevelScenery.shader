Shader "Shootemup/Level Scenery"
{
    Properties { _Flash("Storm illumination",Range(0,1))=0 _Haze("Aerial distance haze",Range(0,1))=0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _Flash,_Haze;
            CBUFFER_END
            struct Input { float4 positionOS:POSITION; float4 color:COLOR; };
            struct Output { float4 positionCS:SV_POSITION; float4 color:COLOR; };
            Output Vert(Input i) { Output o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.color=i.color;return o; }
            half4 Frag(Output i):SV_Target
            {
                float3 distant=lerp(i.color.rgb,float3(.30,.38,.43),_Haze);
                return half4(lerp(distant,float3(.73,.81,.96),_Flash),i.color.a);
            }
            ENDHLSL
        }
    }
}
