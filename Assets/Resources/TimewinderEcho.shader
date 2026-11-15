Shader "Shootemup/Timewinder Echo"
{
    // The Timewinder's echo: a translucent teal copy of the ship where it was a few seconds
    // ago. Glowing rims, thin scan bands rolling backwards and a faint flicker, so it reads as
    // a recording rather than a second ship. _Flare brightens it at the moment of a rewind.
    Properties { _Fade("Fade",Float)=1 _Phase("Phase",Float)=0 _Flare("Flare",Float)=0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+85" }
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
            float _Fade,_Phase,_Flare;
            struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
            struct Varyings {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;};
            Varyings Vert(Attributes v)
            {
                Varyings o;o.world=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);
                o.positionCS.z=lerp(UNITY_NEAR_CLIP_VALUE*o.positionCS.w,o.positionCS.z,.001);
                o.normal=TransformObjectToWorldNormal(v.normalOS);return o;
            }
            half4 Frag(Varyings v):SV_Target
            {
                float3 n=normalize(v.normal);
                // Faces turned away from the camera glow brightest, outlining the silhouette.
                float rim=1-abs(n.z);
                // Scan bands roll upward through the hull, the wrong way round in time.
                float band=smoothstep(.55,.8,frac(v.world.y*7+_Phase*1.6));
                float flicker=.88+.12*sin(_Phase*37)*sin(_Phase*13);
                float3 teal=float3(.22,.92,.84);
                float3 colour=lerp(teal*.75,float3(.75,1,.95),saturate(rim*.9+band*.35+_Flare));
                float alpha=(.2+.5*rim+.22*band)*flicker*(1+_Flare);
                return half4(colour,saturate(alpha)*_Fade);
            }
            ENDHLSL
        }
    }
}
