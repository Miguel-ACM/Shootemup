Shader "Shootemup/PSX Asteroid"
{
    // Brown asteroid rock. A tileable rock texture is projected three ways in object space, so the
    // dense faceted mesh needs no UVs and fragments carry the same stone as their parent. Vertex
    // colours carry the relief shading (dark hollows and craters, lit ridges) and each rock's tint.
    // Keeps the hull contract: _Tint blink, _HitFlash, burn/poison coatings, PSX snapping and 15-bit colour.
    Properties
    {
        _MainTex ("Rock texture", 2D) = "white" {}
        _Tint ("Damage flash tint", Color) = (1,1,1,1)
        _HitFlash ("Local hit flash", Range(0,1)) = 0
        [HideInInspector] _HullStatus ("Hull afflictions", Float) = 0
        _Repeat ("Texture repeats per unit", Float) = .8
        _VertexSnap ("Vertex snap amount", Range(0,1)) = 0.35
        _SnapResolution ("Virtual screen resolution", Vector) = (320,240,0,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "PSXAsteroid"
            Tags { "LightMode"="Universal2D" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_local _ HULL_AFFLICTED
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../Shaders/HullAffliction.hlsl"
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Tint;
                float4 _SnapResolution;
                float _VertexSnap, _Repeat;
                float _HitFlash, _HullStatus;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 objectPosition : TEXCOORD0;
                float3 objectNormal : TEXCOORD1;
                float light : TEXCOORD2;
                float rim : TEXCOORD3;
                half4 color : COLOR;
                #if defined(HULL_AFFLICTED)
                float2 hullPosition : TEXCOORD4;
                #endif
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                float2 screen = output.positionCS.xy / output.positionCS.w;
                float2 resolution = max(_SnapResolution.xy * 0.5, 1.0);
                float2 snapped = floor(screen * resolution + 0.5) / resolution;
                output.positionCS.xy = lerp(screen, snapped, _VertexSnap) * output.positionCS.w;
                output.objectPosition = input.positionOS.xyz;
                output.objectNormal = input.normalOS;
                float3 normal = TransformObjectToWorldNormal(input.normalOS);
                float diffuse = saturate(dot(normal, normalize(float3(-0.45, 0.65, -0.8))));
                output.light = 0.42 + floor(diffuse * 8.0 + 0.5) / 8.0 * 0.88;
                // The camera looks along +z: edge-on facets catch a dusty rim against black space.
                output.rim = pow(saturate(1 + normal.z), 3);
                output.color = input.color;
                #if defined(HULL_AFFLICTED)
                output.hullPosition = HullSurfacePosition(input.positionOS.xyz);
                #endif
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float3 n = normalize(input.objectNormal);
                float3 w = pow(abs(n), 4);
                w /= max(w.x + w.y + w.z, 1e-4);
                float3 p = input.objectPosition * _Repeat;
                half3 rock = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, p.yz).rgb * w.x
                    + SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, p.xz + .37).rgb * w.y
                    + SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, p.xy + .71).rgb * w.z;
                half3 color = rock * input.color.rgb;
                #if defined(HULL_AFFLICTED)
                color = HullAffliction(color, input.hullPosition, _HullStatus);
                #endif
                half alpha = _Tint.a;
                clip(alpha - 0.01);
                color *= _Tint.rgb * input.light;
                color += half3(.62, .45, .30) * input.rim * .34;
                color = lerp(color, half3(1, .88, .63), saturate(_HitFlash) * .88);
                // Five bits per channel, like the ships.
                color = floor(saturate(color) * 31.0 + 0.5) / 31.0;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
