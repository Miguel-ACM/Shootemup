Shader "Shootemup/Level Scenery"
{
    Properties
    {
        _Flash("Storm illumination",Range(0,1))=0
        _Haze("Aerial distance haze",Range(0,1))=0
        [HideInInspector] _ZWrite("Depth write",Float)=1
        [HideInInspector] _TitleLampBanks("Title lamp banks",Vector)=(1,1,1,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite [_ZWrite] Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_local _ TITLE_LAMP_STARTUP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _Flash,_Haze;
            float4 _TitleLampBanks;
            CBUFFER_END
            struct Input { float4 positionOS:POSITION; float4 color:COLOR; };
            struct Output
            {
                float4 positionCS:SV_POSITION;
                float4 color:COLOR;
                #if defined(TITLE_LAMP_STARTUP)
                float worldY:TEXCOORD0;
                #endif
            };
            Output Vert(Input i)
            {
                Output o;
                o.positionCS=TransformObjectToHClip(i.positionOS.xyz);
                o.color=i.color;
                #if defined(TITLE_LAMP_STARTUP)
                o.worldY=TransformObjectToWorld(i.positionOS.xyz).y;
                #endif
                return o;
            }
            half4 Frag(Output i):SV_Target
            {
                float3 distant=lerp(i.color.rgb,float3(.30,.38,.43),_Haze);
                float3 lit=lerp(distant,float3(.73,.81,.96),_Flash);
                #if defined(TITLE_LAMP_STARTUP)
                float middle=smoothstep(-5.2,-.7,i.worldY);
                float far=smoothstep(.7,5.2,i.worldY);
                float exposure=lerp(lerp(_TitleLampBanks.x,_TitleLampBanks.y,middle),_TitleLampBanks.z,far);
                lit*=exposure;
                #endif
                return half4(lit,i.color.a);
            }
            ENDHLSL
        }
    }
}
