Shader "Shootemup/Candy Clouds"
{
    Properties { _Clock("Cloud time",Float)=0 _Seed("Cloud seed",Float)=0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            ZWrite On Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _Clock,_Seed;
            CBUFFER_END
            struct Input { float4 positionOS:POSITION; };
            struct Output {float4 positionCS:SV_POSITION;float2 p:TEXCOORD0;};
            Output Vert(Input i){Output o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.p=i.positionOS.xy;return o;}
            float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float Noise(float2 p)
            {
                float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(Hash(a),Hash(a+float2(1,0)),f.x),lerp(Hash(a+float2(0,1)),Hash(a+1),f.x),f.y);
            }
            float Billow(float2 p){return .7*Noise(p)+.23*Noise(p*2.03+11)+.07*Noise(p*4.1+23);}
            half4 Frag(Output i):SV_Target
            {
                float2 p=i.p*float2(.22,.17)+float2(_Seed,_Clock*.15);
                float n=Billow(p+float2(Noise(p*.7)*1.4,0));
                float clouds=smoothstep(.24,.78,n);
                float rim=saturate((Billow(p+float2(-.13,.20))-n)*5);
                float3 color=lerp(float3(.39,.23,.43),float3(.82,.52,.65),clouds);
                color=lerp(color,float3(.94,.71,.77),rim*.55);
                float veil=Billow(i.p*float2(.11,.3)+float2(_Clock*.04,_Clock*.23+19));
                color=lerp(color,float3(.77,.46,.61),smoothstep(.45,.8,veil)*.22);
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
