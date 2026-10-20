// PSX look for the PSXv2 models in URP 2D (Universal2D pass), after the Render96 recipe:
//  * vertex snapping to the low-res render target grid (PSXPresentation sets _PSXSnapResolution)
//  * affine texture mapping (UVs interpolated without perspective correction)
//  * nearest-neighbour atlas (texture imported as Point, no mipmaps, no compression)
//  * flat, banded directional light multiplied by baked vertex-colour occlusion
//  * atlas row 2 (v 0.625..0.75) is fullbright: engine glows, eyes, reactors
//  * 15-bit colour, optional 4x4 ordered dither (off by default: the screen composite dithers)
// The gameplay camera is orthographic, so w == 1 and affine warping is invisible there;
// _Affine only shows with a perspective camera.
Shader "Shootemup/PSXv2 Lit"
{
    Properties
    {
        _MainTex ("Atlas (Point filtered)", 2D) = "white" {}
        _Tint ("Damage flash tint", Color) = (1,1,1,1)
        _SnapResolution ("Fallback virtual resolution", Vector) = (569,320,0,0)
        _VertexSnap ("Vertex snap", Range(0,1)) = 1
        _Affine ("Affine warp", Range(0,1)) = 1
        _Dither ("Dither", Range(0,1)) = 0
        _LightBands ("Light bands", Float) = 3
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "PSXv2"
            Tags { "LightMode"="Universal2D" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _PSXSnapResolution;
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST, _Tint, _SnapResolution;
                float _VertexSnap, _Affine, _Dither, _LightBands;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 uvw        : TEXCOORD0;   // uv * w, w: divided per pixel = affine
                float2 uvPersp    : TEXCOORD1;
                nointerpolation float light : TEXCOORD2;
                float ao          : TEXCOORD3;
            };
            Varyings Vert(Attributes i)
            {
                Varyings o;
                float4 cs = TransformObjectToHClip(i.positionOS.xyz);
                float2 res = _PSXSnapResolution.x > 0 ? _PSXSnapResolution.xy : _SnapResolution.xy;
                res = max(res * 0.5, 1.0);
                float2 ndc = cs.xy / cs.w;
                cs.xy = lerp(ndc, floor(ndc * res + 0.5) / res, _VertexSnap) * cs.w;
                o.positionCS = cs;
                float2 uv = TRANSFORM_TEX(i.uv, _MainTex);
                o.uvw = float3(uv * cs.w, cs.w);
                o.uvPersp = uv;
                float3 n = TransformObjectToWorldNormal(i.normalOS);
                float d = saturate(abs(dot(n, normalize(float3(-0.45, 0.65, -0.8)))));
                o.light = 0.45 + floor(d * _LightBands + 0.5) / _LightBands * 0.65;
                o.ao = i.color.r;
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                float2 uv = lerp(i.uvPersp, i.uvw.xy / i.uvw.z, _Affine);
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                float glow = step(0.625, i.uvPersp.y) * step(i.uvPersp.y, 0.75);
                c.rgb *= lerp(i.light * i.ao * 1.15, 1.0, glow);
                c *= _Tint;
                uint2 p = (uint2)i.positionCS.xy & 3;
                const float bayer[16] = {0,8,2,10,12,4,14,6,3,11,1,9,15,7,13,5};
                float dth = ((bayer[p.y * 4 + p.x] + 0.5) / 16.0 - 0.5) * _Dither / 31.0;
                c.rgb = floor(saturate(c.rgb + dth) * 31.0 + 0.5) / 31.0;
                return c;
            }
            ENDHLSL
        }
    }
}
