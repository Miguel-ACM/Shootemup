Shader "Shootemup/Gallery guide"
{
    Properties { _Origin("Console origin",Vector)=(0,0,0,0) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 _Origin;
            struct Varyings {float4 position:SV_POSITION;float travel:TEXCOORD0;half3 color:COLOR;};
            Varyings vert(float4 position:POSITION, half4 color:COLOR)
            {
                Varyings o;
                o.position=TransformObjectToHClip(position.xyz);
                o.travel=abs(position.x-_Origin.x)+abs(position.y-_Origin.y);
                o.color=color.rgb;
                return o;
            }
            half4 frag(Varyings i):SV_Target
            {
                half pulse=pow(saturate(1-abs(frac(i.travel*.19-_Time.y*.72)-.5)*2),5);
                return half4(lerp(i.color*.78,half3(.40,.75,1),pulse),1);
            }
            ENDHLSL
        }
    }
}
