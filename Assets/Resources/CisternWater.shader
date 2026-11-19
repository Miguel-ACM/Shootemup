Shader "Shootemup/Cistern Water"
{
    Properties
    {
        _LakeWake("Wake from the cistern", Float) = 0
        _FlowTime("Current time", Float) = 0
        _ScrollDistance("Channel travel", Float) = 0
        [NoScaleOffset] _SurfaceMap("Water slopes and foam", 2D) = "gray" {}
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Cull Off ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_SurfaceMap); SAMPLER(sampler_SurfaceMap);

            CBUFFER_START(UnityPerMaterial)
            float _LakeWake, _FlowTime, _ScrollDistance;
            CBUFFER_END
            struct Input { float4 vertex : POSITION; float4 color : COLOR; };
            struct Varyings { float4 position : SV_POSITION; float2 local : TEXCOORD0; float2 world : TEXCOORD1; float4 color : COLOR; };

            Varyings Vert(Input i)
            {
                Varyings o;
                o.position = TransformObjectToHClip(i.vertex.xyz);
                o.local = i.vertex.xy;
                o.world = TransformObjectToWorld(i.vertex.xyz).xy;
                o.color = i.color;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                if (i.color.a < .01)
                    return half4(i.color.rgb, 1);
                float t = _FlowTime;
                // The bed scrolls with the banks. Both surface layers also run
                // downstream relative to that bed. Every Y frequency tiles over 80 units.
                float2 bed = float2(i.local.x, i.world.y + _ScrollDistance);
                float2 fastUV = bed * float2(.145,.0875) + float2(0,t*.21);
                float2 slowUV = bed * float2(.22,.125) + float2(.37,t*.102);
                float2 bendUV = bed * float2(.07,.0375) + float2(0,t*.041);
                float2 bend = SAMPLE_TEXTURE2D(_SurfaceMap,sampler_SurfaceMap,bendUV).rg - .5;
                float3 run = SAMPLE_TEXTURE2D(_SurfaceMap,sampler_SurfaceMap,fastUV + bend*.095).rgb;
                float3 slack = SAMPLE_TEXTURE2D(_SurfaceMap,sampler_SurfaceMap,slowUV - bend*.06).rgb;
                float margin = smoothstep(1.85,3.6,abs(i.local.x));
                float3 surface = lerp(run,slack,.17 + .59*margin);
                float2 slope = surface.rg * 2 - 1;
                float face = dot(slope,float2(-.35,.94));
                float3 capillary = SAMPLE_TEXTURE2D(_SurfaceMap,sampler_SurfaceMap,
                    bed*float2(.24,.325)+float2(.63,t*.68)+slope*.07).rgb;
                float crest = face + (capillary.g-.5)*.30;
                float sheen = smoothstep(.28,.58,crest);
                float glint = smoothstep(.53,.74,capillary.g) * smoothstep(.14,.41,face);
                float filament = surface.b * (.34 + .54*margin);
                float3 color = i.color.rgb * float3(.66,1.00,.85);
                color += float3(.038,.104,.077) * (.32 + .68*saturate(face+.4));
                color += float3(.082,.144,.112)*sheen + float3(.085,.145,.15)*glint;
                color += float3(.15,.20,.16)*filament;

                // The discharge bends downstream into a broken wake at each spout.
                float side = i.local.x < 0 ? -1 : 1;
                float2 outlet = i.local - float2(side*2.45,side*4.85-.46);
                outlet.x += side * .42 * saturate(-outlet.y);
                float dist = length(outlet * float2(1.27, outlet.y < 0 ? .72 : 1.75));
                float arc = smoothstep(.36,.87,cos(dist*17 - t*5.4 + slope.y*1.6));
                float jet = exp(-dist*2.15) * arc;
                float2 lamp = i.local - float2(side*2.89,side*4.85+.62);
                lamp.x += slope.x*.52;
                lamp *= float2(2.25,.82);
                float amber = exp(-dot(lamp,lamp)) * (.12 + .64*sheen + .56*glint);
                color += (float3(.19,.27,.25)*jet + float3(.39,.23,.075)*amber)*(1-_LakeWake);
                float radius = length(i.local * float2(1,1.13));
                float wake = smoothstep(.65,.96,cos(radius*10-t*3+slope.y));
                color += float3(.058,.13,.14)*wake*smoothstep(.95,1.7,radius)*(1-smoothstep(3.3,4.3,radius))*_LakeWake;
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
