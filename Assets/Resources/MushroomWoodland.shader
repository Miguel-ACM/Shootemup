Shader "Shootemup/Mushroom Woodland"
{
    Properties
    {
        _MainTex("Woodland leaf clusters", 2D) = "white" {}
        _SectionOrigin("Section origin", Float) = 0
        _WindTime("Canopy wind", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Cull Off ZWrite On Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float _SectionOrigin, _WindTime;
            CBUFFER_END
            struct Input { float3 vertex:POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Interpolated { float4 position:SV_POSITION; float3 ground:TEXCOORD0; half4 color:COLOR; float2 uv:TEXCOORD1; };
            Interpolated Vert(Input v)
            {
                Interpolated o;
                o.ground = float3(v.vertex.xy + float2(0, _SectionOrigin), -v.vertex.z);
                if (v.uv.x >= 0)
                    v.vertex.x += .021*o.ground.z*sin(_WindTime*1.25 + o.ground.x*.85 + o.ground.y*.785398);
                o.position = TransformObjectToHClip(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }
            float Hash(float2 p)
            {
                p = frac(p * float2(.1031,.1137));
                return frac((p.x+p.y) * (19.7+dot(p,p.yx+33.33)));
            }
            // Repeat the lighting over the full 80-unit forest circuit. Overhanging
            // branches get the same shafts of light as the next section's ground.
            float Noise(float2 p, float period)
            {
                float2 lower = floor(p), upper = lower + 1, f = frac(p);
                lower.y -= floor(lower.y/period)*period;
                upper.y -= floor(upper.y/period)*period;
                f *= f*(3-2*f);
                return lerp(lerp(Hash(lower),Hash(float2(upper.x,lower.y)),f.x),
                    lerp(Hash(float2(lower.x,upper.y)),Hash(upper),f.x),f.y);
            }
            half LeafRelief(float2 p)
            {
                // Atlas quadrants 0 and 1 are the project's existing deciduous leaf
                // and needle clusters, including their internally drawn twigs.
                float2 cell = floor(p), uv = frac(p);
                cell.y -= floor(cell.y/80)*80;
                float variant = Hash(cell);
                if (variant < .5) uv.x = 1-uv.x;
                uv = clamp(uv,.012,.988)*.5 + float2(variant > .64 ? .5 : 0, 0);
                half4 leaves = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv);
                return leaves.a*(leaves.g*2.6-.56);
            }
            half4 Frag(Interpolated i):SV_Target
            {
                float2 p = i.ground.xy;
                if (i.color.a < .99) return i.color;
                float drift = Noise(p*.25,20);
                float sun = Noise(p*.5 + float2(drift*1.4, drift),40);
                float lightPool = smoothstep(.28,.77, sun*.78 + drift*.22);
                if (i.uv.x >= 0)
                {
                    half4 spray = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
                    clip(spray.a-.49);
                    half3 sunlitLeaf = spray.rgb*i.color.rgb;
                    sunlitLeaf *= lerp(half3(.78,.91,1.10),half3(1.24,1.12,.93),lightPool);
                    return half4(sunlitLeaf + half3(.012,.022,.024),1);
                }
                half leaf = LeafRelief(p)*.66h + LeafRelief(p*2 + float2(.46,.83))*.40h;
                float humus = Noise(p*3 + float2(5,3),240);
                half3 shade = i.color.rgb * half3(.75,.87,1.02) + half3(.012,.023,.027);
                half3 sunlit = i.color.rgb * half3(1.12,1.15,.90) + half3(.038,.049,.017);
                half3 pigment = lerp(shade,sunlit,lightPool);
                pigment *= 1 + leaf*.23 + (humus-.5)*.14;
                return half4(pigment,1);
            }
            ENDHLSL
        }
    }
}
