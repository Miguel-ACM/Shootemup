Shader "Shootemup/Caldera basalt"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Cull Off ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Input {float4 p:POSITION;float4 color:COLOR;};
            struct Output {float4 p:SV_POSITION;float3 local:TEXCOORD0;float4 color:COLOR;};
            Output Vert(Input i){Output o;o.p=TransformObjectToHClip(i.p.xyz);o.local=i.p.xyz;o.color=i.color;return o;}
            float Hash(float2 p){float3 q=frac(float3(p.xyx)*.1031);q+=dot(q,q.yzx+33.33);return frac((q.x+q.y)*q.z);}
            float Noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(a),Hash(a+float2(1,0)),f.x),lerp(Hash(a+float2(0,1)),Hash(a+1),f.x),f.y);}
            half4 Frag(Output i):SV_Target
            {
                float2 p=i.local.xy;
                float grain=Noise(p*18.0),mineral=Noise(p*1.7);
                float crease=1-smoothstep(.013,.039,abs(Noise(p*3.3+19)-.51));
                float shade=(.84+grain*.23+mineral*.10)*(1-crease*.24);
                return half4(i.color.rgb*shade,1);
            }
            ENDHLSL
        }
    }
}
