Shader "Shootemup/Level Atmosphere"
{
    Properties
    {
        _Tint("Atmosphere tint",Color)=(.4,.5,.6,.2)
        _Kind("0 mist, 1 deep storm, 2 near storm",Float)=0
        _Flash("Lightning",Range(0,1))=0
        _Drift("Cloud travel",Vector)=(0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _Tint,_Drift;
            float _Kind,_Flash;
            CBUFFER_END
            struct Input { float4 positionOS:POSITION; };
            struct Output { float4 positionCS:SV_POSITION; float2 local:TEXCOORD0; };
            Output Vert(Input i) { Output o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.local=i.positionOS.xy;return o; }
            float Hash(float2 p) { p=frac(p*float2(123.34,345.45));p+=dot(p,p+34.345);return frac(p.x*p.y); }
            float Noise(float2 p)
            {
                float2 cell=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(Hash(cell),Hash(cell+float2(1,0)),f.x),lerp(Hash(cell+float2(0,1)),Hash(cell+1),f.x),f.y);
            }
            float Clouds(float2 p) { return Noise(p)*.57+Noise(p*2.03+7.1)*.28+Noise(p*4.11+19.3)*.15; }
            half4 Frag(Output i):SV_Target
            {
                float2 p=i.local*float2(.22,.18)+_Drift.xy;
                if(_Kind<.5)
                {
                    float mist=Clouds(p*float2(.5,2.4));
                    return half4(_Tint.rgb,smoothstep(.36,.73,mist)*_Tint.a);
                }
                float n=Clouds(p),billow=Clouds(p*1.7+12.5);
                float shape=smoothstep(.23,.78,n+.13*billow);
                float ridge=saturate((Noise(p+float2(.08,.13))-Noise(p-float2(.08,.13)))*3+.3);
                float3 dark=float3(.025,.042,.065);
                float3 cloud=lerp(dark,_Tint.rgb,shape*.83+ridge*.12);
                cloud=lerp(cloud,float3(.64,.75,.91),_Flash*(.38+shape*.62));
                float alpha=_Kind<1.5 ? 1 : smoothstep(.42,.74,n)*_Tint.a;
                return half4(cloud,alpha);
            }
            ENDHLSL
        }
    }
}
