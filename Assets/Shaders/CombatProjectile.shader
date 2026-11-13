// Keep projectile artwork intact while ignoring hull depth. Bullet sorting places it over ships.
Shader "Shootemup/Combat Projectile"
{
    Properties
    {
        [PerRendererData] _MainTex ("Projectile artwork",2D)="white" {}
        _Color ("Tint",Color)=(1,1,1,1)
        _SpriteTint ("Code tint",Color)=(1,1,1,1)
        _Opacity ("Code opacity",Range(0,1))=1
        _Neutralize ("Legacy grayscale",Float)=0
        _Burn ("Burn coating",Float)=0
        _Poison ("Poison coating",Float)=0
        _PoisonTint ("Poison jade",Color)=(.42,.70,.58,1)
        _Gloss ("Enamel highlights",Range(0,1))=.7
        _Emission ("Colored radiance",Range(0,4))=2.6
        _Dark ("Dark tone",Float)=0
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
                float4 _PoisonTint;
                float _Neutralize,_Burn,_Poison,_Gloss,_Emission,_Dark,_Opacity;
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
                // Dark tone (Ikaruga-like): the artwork's dark outline glows in the district hue
                // and its body turns deep. The highlight is only a slightly lighter body shade;
                // a pale glint or gloss on the dark body read as a stray blob (Miguel).
                half outline=1-smoothstep(.12,.26,brightness);
                half3 hue=_SpriteTint.rgb;
                half3 deep=lerp(hue*(.16+.14*(1-brightness)),hue*1.1+.1,outline);
                deep=lerp(deep,hue*.34,smoothstep(.97,1,brightness));
                color.rgb=lerp(lerp(color.rgb,brightness.xxx,_Neutralize)*hue,deep,_Dark);
                color*=v.color;
                brightness=max(color.r,max(color.g,color.b));
                color.rgb=lerp(color.rgb,half3(1,.15,.07)*brightness,_Burn);
                // UVs stay local to the artwork when Unity batches sprite vertices in world space.
                float toxic=_Poison*(1-_Burn*step(.5,v.uv.y));
                color.rgb=lerp(color.rgb,_PoisonTint.rgb*brightness,toxic);
                half3 radiance=color.rgb;
                float facet=saturate(1-abs(dot(v.uv,float2(.85,.45))-.49)*11);
                float2 light=(v.uv-float2(.34,.69))*float2(5,4.5);
                float glint=saturate(1-dot(light,light));
                half shine=enamel*_Gloss*(facet*facet*.25+glint*glint*.65)*(1-_Dark);
                color.rgb=lerp(color.rgb,half3(1,1,1),shine);
                // HDR energy is collected by one shared bloom, never a light or halo per round.
                // Keep the original silhouette and dark ink out of the emissive core.
                color.rgb+=lerp(radiance*enamel*_Emission,radiance*outline*1.2,_Dark);
                color.a*=_Opacity;
                clip(color.a-.005);return color;
            }
            ENDHLSL
        }
    }
}
