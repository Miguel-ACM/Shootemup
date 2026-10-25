Shader "Shootemup/Ship Decoy"
{
    Properties { _Fade("Fade",Float)=1 _Phase("Phase",Float)=0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+90" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float _Fade,_Phase;
            struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
            struct Varyings {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;};
            Varyings Vert(Attributes v)
            {
                Varyings o;o.world=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);
                // Match the actual ship's foreground placement so terrain cannot cut through the copy.
                o.positionCS.z=lerp(UNITY_NEAR_CLIP_VALUE*o.positionCS.w,o.positionCS.z,.001);
                o.normal=TransformObjectToWorldNormal(v.normalOS);return o;
            }
            half4 Frag(Varyings v):SV_Target
            {
                float band=step(.35,frac(v.world.y*9-_Phase*2));
                float light=.75+.25*abs(dot(normalize(v.normal),normalize(float3(-.4,.6,-1))));
                return half4(float3(.3,.85,.95)*light,(.25+.45*band)*_Fade);
            }
            ENDHLSL
        }
    }
}
