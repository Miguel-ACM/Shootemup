// Keep projectile artwork intact while ignoring hull depth. Bullet sorting places it over ships.
Shader "Shootemup/Combat Projectile"
{
    Properties
    {
        [PerRendererData] _MainTex ("Projectile artwork",2D)="white" {}
        _Color ("Tint",Color)=(1,1,1,1)
        _SpriteTint ("Code tint",Color)=(1,1,1,1)
        _Neutralize ("Legacy grayscale",Float)=0
        _Burn ("Burn coating",Float)=0
        _Poison ("Poison coating",Float)=0
        _Gloss ("Enamel highlights",Range(0,1))=.7
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+100" "CanUseSpriteAtlas"="True" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _SpriteTint;
                float _Neutralize,_Burn,_Poison,_Gloss;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR; };
            Varyings Vert(Attributes v)
            {
                Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.uv=v.uv;o.color=v.color*_Color;return o;
            }
            half4 Frag(Varyings v):SV_Target
            {
                half4 color=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,v.uv);
                half brightness=max(color.r,max(color.g,color.b));
                // Shade only existing bright ink: alpha, the dark rim and the collider stay intact.
                half enamel=smoothstep(.22,.8,brightness);
                color.rgb=lerp(color.rgb,brightness.xxx,_Neutralize)*_SpriteTint.rgb;
                color*=v.color;
                brightness=max(color.r,max(color.g,color.b));
                color.rgb=lerp(color.rgb,half3(1,.15,.07)*brightness,_Burn);
                // UVs stay local to the artwork when Unity batches sprite vertices in world space.
                float toxic=_Poison*(1-_Burn*step(.5,v.uv.y));
                color.rgb=lerp(color.rgb,half3(.22,1,.2)*brightness,toxic);
                float facet=saturate(1-abs(dot(v.uv,float2(.85,.45))-.49)*11);
                float2 light=(v.uv-float2(.34,.69))*float2(5,4.5);
                float glint=saturate(1-dot(light,light));
                half shine=enamel*_Gloss*(facet*facet*.25+glint*glint*.65);
                color.rgb=lerp(color.rgb,half3(1,1,1),shine);
                clip(color.a-.005);return color;
            }
            ENDHLSL
        }
    }
}
