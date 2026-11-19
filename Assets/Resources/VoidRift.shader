Shader "Shootemup/Void Rift"
{
    Properties { _Open("Opening",Range(0,1))=0 _Clock("Clock",Float)=0 _Arrival("Arrival",Float)=0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _Open,_Clock,_Arrival;
            CBUFFER_END
            float4 _VoidRouteLight;
            struct I{float4 p:POSITION;float2 uv:TEXCOORD0;};
            struct O{float4 p:SV_POSITION;float2 xy:TEXCOORD0;};
            O Vert(I i){O o;o.p=TransformObjectToHClip(i.p.xyz);o.xy=i.uv*2-1;return o;}
            half4 Frag(O i):SV_Target
            {
                // Three offset folds peel apart in sequence. Each visible face is bounded
                // by straight creases, with empty space between the outer paper edges.
                float opening = smoothstep(0,1,_Open);
                float hand = lerp(-1,1,_Arrival);
                float twist = hand * (.18 + (1-opening)*.68);
                float sn,cs; sincos(twist,sn,cs);
                float2 p = mul(float2x2(cs,-sn,sn,cs),i.xy);
                float3 tint = _VoidRouteLight.a > .5 ? _VoidRouteLight.rgb : float3(.72,.34,1);
                float3 color = 0;
                float alpha = 0;
                [unroll] for (int layer=2;layer>=0;layer--)
                {
                    float t = saturate((opening-layer*.10)/(1-layer*.10));
                    float2 q = p - float2(hand*layer*.090,layer*.055)*t;
                    float width = (.40-layer*.065)*max(.015,t*t);
                    float h = (.82-layer*.08)*max(.02,sqrt(t));
                    float d = abs(q.x+.15*q.y)/width + abs(q.y)/h - 1;
                    float aa = max(fwidth(d),.012);
                    float face = 1-smoothstep(-aa,aa,d);
                    float rim = 1-smoothstep(aa,aa*2.8,abs(d));
                    float facet = step(q.x+.15*q.y,0);
                    float crease = 1-smoothstep(.003,.018,abs(q.x+.15*q.y));
                    float sweep = saturate(1-abs(q.y/h - (t*2-1)*hand)*4);
                    float3 panel = tint * (.032+facet*.085+layer*.05+crease*.28);
                    panel += lerp(tint,1,.38)*rim*(.50+.50*sweep);
                    color = lerp(color,panel,max(face,rim));
                    alpha = max(alpha,max(face*.92,rim)*smoothstep(0,.15,t));
                }
                return half4(color,alpha);
            }
            ENDHLSL
        }
    }
}
