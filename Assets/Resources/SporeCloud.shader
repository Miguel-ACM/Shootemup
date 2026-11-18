Shader "Shootemup/Spore Cloud"
{
    Properties { _Clock("Spore time",Float)=0 _Strength("Opacity",Float)=1 _ShipAura("Ship afterglow",Float)=0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Cull Off ZWrite Off Blend One OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _Clock,_Strength,_ShipAura;
            CBUFFER_END
            struct Varyings { float4 position:SV_POSITION; float2 local:TEXCOORD0; };
            Varyings Vert(float4 vertex:POSITION)
            {
                Varyings v;
                v.position = TransformObjectToHClip(vertex.xyz);
                v.local = vertex.xy;
                return v;
            }
            float Hash(float2 p) { p = frac(p * float2(123.34,345.45)); p += dot(p,p+34.345); return frac(p.x*p.y); }
            float Noise(float2 p)
            {
                float2 c=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(Hash(c),Hash(c+float2(1,0)),f.x),lerp(Hash(c+float2(0,1)),Hash(c+1),f.x),f.y);
            }
            half4 Frag(Varyings v):SV_Target
            {
                float2 p = v.local;
                if (_ShipAura>.5)
                {
                    float r=length(p*float2(1,.92));
                    float halo=(1-smoothstep(.19,1,r))*_Strength;
                    float3 violet=lerp(float3(.29,.045,.57),float3(.66,.20,.85),saturate(1-r));
                    return half4(violet*halo,halo);
                }
                float2 scroll = float2(_Clock*.16,_Clock*.24);
                float fold = Noise(p*3.7-scroll);
                float veil = Noise(p*7.5+scroll+fold*1.3);
                float distance = length(p)+(fold-.5)*.17;
                float body = 1-smoothstep(.70,.98,distance);
                float rim = smoothstep(.61,.73,distance)*(1-smoothstep(.74,.92,distance));
                float grain = step(.91,Hash(floor((p+scroll*.14)*32))) * smoothstep(.51,.68,veil);
                float3 color = lerp(float3(.25,.08,.39),float3(.66,.27,.81),fold*.68+veil*.22);
                color += float3(.12,.055,.16)*rim + grain*.20;
                float alpha = _Strength*(body*(.48+fold*.28)+rim*.17);
                return half4(color*alpha,alpha);
            }
            ENDHLSL
        }
    }
}
