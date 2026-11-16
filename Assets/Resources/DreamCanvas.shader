Shader "Shootemup/Dream Canvas"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Cull Off ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Input { float4 position:POSITION; half4 ink:COLOR; };
            struct Varyings { float4 position:SV_POSITION; half4 ink:COLOR; float2 surface:TEXCOORD0; };
            Varyings Vert(Input i)
            {
                Varyings o;
                o.position = TransformObjectToHClip(i.position.xyz);
                o.surface = TransformObjectToWorld(i.position.xyz).xy;
                o.ink = i.ink;
                return o;
            }
            float Hash(float2 p) { p=frac(p*float2(123.34,345.45)); p+=dot(p,p+34.345); return frac(p.x*p.y); }
            float Noise(float2 p)
            {
                float2 c=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(Hash(c),Hash(c+float2(1,0)),f.x),lerp(Hash(c+float2(0,1)),Hash(c+1),f.x),f.y);
            }
            half4 Frag(Varyings i):SV_Target
            {
                float2 p = i.surface;
                float2 grain = float2(p.x*.73+p.y*.68,p.y*.73-p.x*.68);
                float brush = Noise(grain*float2(1.9,9.8))*.67 + Noise(grain*float2(5.9,16.3))*.33;
                float canvas = Hash(floor(p*39))-.5;
                float3 pigment = i.ink.rgb * (.91 + brush*.19);
                pigment += float3(.022,.018,.023)*(brush-.28+canvas*.52);
                return half4(pigment,1);
            }
            ENDHLSL
        }
    }
}
