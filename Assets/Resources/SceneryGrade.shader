Shader "Shootemup/Scenery Grade"
{
    // A full-view grade for district conditions, drawn between the scenery and every ship and
    // bullet. One material multiplies the scenery (dusk, night, storm light); a second adds a
    // soft glow from the condition's side. Alpha is untouched, so composite masks survive.
    Properties
    {
        _Near("Colour on the condition's side",Color)=(1,1,1,1)
        _Far("Colour on the far side",Color)=(1,1,1,1)
        _Side("Condition side (-1 left, 1 right)",Float)=1
        _Reach("Gradient reach across the view",Range(.05,1))=1
        _Vignette("Corner falloff",Range(0,1))=0
        _Strength("Fade",Range(0,1))=1
        _Neutral("Colour at zero strength",Color)=(1,1,1,1)
        [HideInInspector] _SrcBlend("Source blend",Float)=2
        [HideInInspector] _DstBlend("Destination blend",Float)=0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Blend [_SrcBlend] [_DstBlend]
            ColorMask RGB
            ZWrite Off ZTest LEqual Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _Near,_Far,_Neutral;
            float _Side,_Reach,_Vignette,_Strength,_SrcBlend,_DstBlend;
            CBUFFER_END
            struct Input { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Output { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            Output Vert(Input i)
            {
                Output o;
                o.positionCS=TransformObjectToHClip(i.positionOS.xyz);
                o.uv=i.uv;
                return o;
            }
            half4 Frag(Output i):SV_Target
            {
                // 1 at the condition's edge, falling to 0 once the reach is crossed.
                float across=_Side>0 ? i.uv.x : 1-i.uv.x;
                float toward=smoothstep(1-_Reach,1,across);
                float3 colour=lerp(_Far.rgb,_Near.rgb,toward);
                float2 centred=i.uv*2-1;
                float corner=saturate(dot(centred,centred)*.5);
                colour=lerp(colour,colour*(1-_Vignette),corner*corner);
                return half4(lerp(_Neutral.rgb,colour,_Strength),1);
            }
            ENDHLSL
        }
    }
}
