Shader "Shootemup/Continuous Beam"
{
    Properties { _Color ("Tint",Color)=(1,1,1,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+110" }
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
            float4 _Color;
            struct Attributes { float4 positionOS:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR; };
            Varyings Vert(Attributes v)
            {
                Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.uv=v.uv;o.color=v.color*_Color;return o;
            }
            half4 Frag(Varyings v):SV_Target
            {
                float across=abs(v.uv.y*2-1);
                float flow=.88+.07*sin(v.uv.x*61-_Time.y*31)+.05*sin(v.uv.x*127+_Time.y*23);
                float edge=1-smoothstep(.55,1,across);
                float core=1-smoothstep(.0,.42,across);
                half3 color=lerp(v.color.rgb*.65,v.color.rgb,edge);
                color=lerp(color,half3(.65,.88,1),core*.38);
                return half4(color,v.color.a*edge*flow);
            }
            ENDHLSL
        }
    }
}
