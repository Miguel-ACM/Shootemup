Shader "Shootemup/Witness penumbra"
{
    Properties { _Motion ("Pose", Vector) = (0,0,0,0) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask RGB
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 _Motion;
            struct Varyings { float4 position : SV_POSITION; float2 hullXY : TEXCOORD0; };
            Varyings vert(float4 position : POSITION)
            {
                Varyings o;
                o.position = TransformObjectToHClip(position.xyz);
                o.hullXY = position.xy * float2(8.8,11.2) + float2(0,-.65);
                return o;
            }
            // Negative inside a tapered stroke. Four strokes give the veil
            // distinct sources on the hem, chest, mantle and hood.
            float stroke(float2 p, float2 a, float2 b, float wa, float wb)
            {
                float2 ab = b-a;
                float u = saturate(dot(p-a,ab)/dot(ab,ab));
                return length(p-a-u*ab)-lerp(wa,wb,u);
            }
            half4 frag(Varyings input) : SV_Target
            {
                float2 p = input.hullXY;
                float t = _Motion.x;
                float side = p.x < 0 ? -1 : 1;
                float rise = p.y - .88*t;
                float curl = sin(3.4*rise + 1.7*p.x + .65*sin(p.y*2.1-t));
                float eddy = sin(5.2*p.x - 2.5*rise + 1.35*curl);
                float2 q = float2(abs(p.x - _Motion.z*saturate(-p.y/4)),p.y);
                q.x += .095*curl + .07*eddy;
                float hem = stroke(q,float2(.38,-4.08),float2(0,-2.85),.86,.93);
                float chest = stroke(q,float2(0,-2.72),float2(.42,-.47),.76,.80);
                float mantle = stroke(q,float2(.43,-.36),float2(1.95,-1.73-.34*side+_Motion.y),.68,.26);
                float hood = stroke(q,float2(.10,.27),float2(.24,1.77),.71,.28);
                float d = min(min(hem,chest),min(mantle,hood));
                // Narrow folds climb through the outer veil. Their ends dissolve
                // at different heights around the hood and mantle.
                float flow = sin(9*q.x - 2.5*rise + 2*curl + .55*side);
                float fold = smoothstep(.10,.96,flow);
                float width = .84 + .21*curl + .13*eddy + .25*_Motion.w;
                float haze = 1-smoothstep(-.38,width+.35,d);
                float silk = (1-smoothstep(.08,width,d)) * fold;
                float3 ink = lerp(float3(.15,.105,.25),float3(.46,.40,.63),fold);
                float alpha = (.42*haze + .24*silk) * (1 + .15*_Motion.w);
                return half4(ink,alpha);
            }
            ENDHLSL
        }
    }
}
