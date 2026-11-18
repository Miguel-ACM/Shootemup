Shader "Shootemup/Dream Canvas"
{
    Properties
    {
        _MainTex("Painted procession", 2D) = "white" {}
        _Studies("Sketchbook", 2D) = "black" {}
        _Drawings("Panel studies", Vector) = (0,1,2,0)
        [HideInInspector] _CanvasTime("Canvas time", Float) = 0
    }
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
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_Studies); SAMPLER(sampler_Studies);
            CBUFFER_START(UnityPerMaterial)
            float4 _Drawings;
            float _CanvasTime;
            CBUFFER_END
            struct Input { float4 p:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 p:SV_POSITION; float2 uv:TEXCOORD0; };
            Varyings Vert(Input i)
            {
                Varyings o;
                o.p = TransformObjectToHClip(i.p.xyz);
                o.uv = i.uv;
                return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float turn = i.uv.y*6.2831853;
                // Broad folds pass through the drawings. Both waves meet across the scroll loop.
                float across = (i.uv.x - .5) * 3.14159265;
                float swell = sin(turn*4 + _CanvasTime*.96 + .72*sin(across*2 - _CanvasTime*.40));
                float curl = sin(turn*8 - _CanvasTime*.71 + across*1.5);
                float margin = smoothstep(.08, .58, abs(i.uv.x - .5));
                float2 bend = float2(.024*swell + .009*curl,
                    .0048*sin(turn*2)*cos(across*2 + swell - _CanvasTime*.63));
                float2 uv = i.uv + bend * lerp(.67, 1.25, margin);
                half3 paint = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv).rgb;
                // Paint into three reserved pockets between the large figures. Object-space UVs
                // move the overpainting with the same folded paper as its original brushwork.
                float panel = floor(frac(uv.y)*4);
                float2 p = float2(1-abs(frac(uv.x*.5)*2-1), 1-frac(uv.y*4));
                if (fmod(panel,2)<.5) p.x=1-p.x;
                float slot = p.y<.50 ? 0 : p.y<.82 ? 1 : 2;
                float id = slot<.5 ? _Drawings.x : slot<1.5 ? _Drawings.y : _Drawings.z;
                float2 centre = slot<.5 ? float2(.755,.405) : slot<1.5 ? float2(.245,.685) : float2(.54,.904);
                float turnOfStudy = frac(id*.618+_Drawings.w)-.5;
                centre.x += turnOfStudy*.105;
                float2 q=(p-centre)*float2(12.8,20);
                q=float2(q.x+.36*turnOfStudy*q.y,q.y-.36*turnOfStudy*q.x);
                q/=3.05+.35*frac(id*.41);
                float2 paper=float2(q.x,-q.y)+.5;
                if (all(paper>0) && all(paper<1))
                {
                    float2 cell=float2(fmod(id,4),3-floor(id/4));
                    half4 glyph=SAMPLE_TEXTURE2D(_Studies,sampler_Studies,(cell+paper)*.25);
                    paint=lerp(paint,glyph.rgb,glyph.a*.85);
                }
                float grazing = .86 + .045*cos(uv.y*6.2831853*11 + uv.x*4 + swell);
                return half4(paint*grazing,1);
            }
            ENDHLSL
        }
    }
}
