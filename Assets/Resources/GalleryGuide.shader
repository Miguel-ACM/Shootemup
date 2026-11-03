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
            struct Varyings {float4 position:SV_POSITION;float travel:TEXCOORD0;};
            Varyings vert(float4 position:POSITION)
            {
                Varyings o;
                o.position=TransformObjectToHClip(position.xyz);
                o.travel=abs(position.x-_Origin.x)+abs(position.y-_Origin.y);
                return o;
            }
            half4 frag(Varyings i):SV_Target
            {
                half pulse=pow(saturate(1-abs(frac(i.travel*.19-_Time.y*.72)-.5)*2),5);
                return half4(.64+.36*pulse,.035+.28*pulse,.018+.12*pulse,1);
            }
            ENDHLSL
        }
    }
}
