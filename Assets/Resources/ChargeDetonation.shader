Shader "Shootemup/Charge detonation"
{
    Properties { _BlastState ("Age, lifetime, full charge", Vector) = (0,1,0,0) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Blend One OneMinusSrcAlpha
            Cull Off ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BlastState;
            CBUFFER_END
            struct Input { float4 position : POSITION; float2 uv : TEXCOORD0; };
            struct Output { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };
            Output Vert(Input v)
            {
                Output o;
                o.position = TransformObjectToHClip(v.position.xyz);
                o.uv = v.uv;
                return o;
            }

            // Fold a continuous sheet of plasma back into itself. The outer folds run
            // ahead of a ragged cavity which burns through the sheet from the centre.
            half4 Plume(float r, float angle, float age, float full)
            {
                float t = saturate(age / (.47 + .15 * full));
                float curl = angle + .43 * r * r + .48 * t + .19 * sin(3 * angle - 4 * r);
                float folds = sin(5 * curl - 3.4 * r) + .53 * sin(8 * curl + 4.2 * r + .9);
                float fray = sin(13 * curl - 5 * r + 8 * t);
                float front = .75 + .13 * folds + .055 * fray;
                float cavity = -.25 + 1.55 * t + .11 * folds - .08 * fray;
                float shell = min(front - r, r - cavity);
                float opacity = smoothstep(-.012,.012,shell) * (1 - smoothstep(.38,1,t));

                // Uneven tongues of incandescent gas roll through the blue outer skin.
                float veins = .5 + .5 * sin(7 * curl - 10 * r + 3 * t + folds);
                float heat = (.88 - r) + .19 * folds + .25 * veins - .39 * t;
                half3 ink = lerp(half3(.025,.14,.30), half3(.035,.56,.67), smoothstep(.07,.15,heat));
                ink = lerp(ink, half3(.38,.91,.83), smoothstep(.29,.35,heat));
                ink = lerp(ink, half3(1,.78,.39), smoothstep(.51,.57,heat));
                ink = lerp(ink, half3(1,.98,.82), smoothstep(.73,.80,heat));
                return half4(ink * opacity, opacity);
            }

            half4 Frag(Output i) : SV_Target
            {
                float2 p = i.uv * 2 - 1;
                float r = length(p);
                clip(1 - r);
                float age = _BlastState.x, life = _BlastState.y, full = _BlastState.z;
                float angle = atan2(p.y, p.x);
                float fade = smoothstep(0, .20, life - age);

                // Broken arcs at the live damage boundary, with a travelling discharge inside.
                float corrugation = sin(5 * angle - 7 * age) + .55 * sin(3 * angle + 5 * age);
                float rim = saturate(1 - abs(r - .97) / .03);
                float arcs = .17 + .66 * smoothstep(-.55,.6,corrugation);
                float beat = frac(max(0, age - .12) * (2.45 + .4 * full));
                float trail = saturate(1 - abs(r - (.16 + .76 * beat + .035 * corrugation)) / .068);
                trail *= sin(PI * beat) * smoothstep(-.55,.7,corrugation) * .71;
                float opacity = max(rim * arcs, trail);
                half4 color = half4(lerp(half3(.11,.62,.69), half3(.51,.91,.86), rim) * opacity, opacity);

                if (age < .68)
                {
                    half4 cloud = Plume(r, angle, age, full);
                    color = cloud + color * (1 - cloud.a);
                }

                // Short, curved filaments shed from the front and wink out inside the rim.
                float lane = (angle + .26 * r * r + .13 * sin(2 * angle)) * (5 / TWO_PI);
                float sector = floor(lane + .5);
                float drift = .5 + .5 * sin(sector * (TWO_PI / 5) + .7);
                float flight = frac(age * (1.8 + .4 * drift) + drift);
                float tip = .32 + .61 * flight;
                float width = abs(lane - sector) * max(r,.15);
                float filament = saturate(1 - width / .024) * saturate(1 - abs(r-tip) / .11);
                filament *= sin(PI * flight) * smoothstep(.09,.28,age) * .86;
                half3 ember = lerp(half3(.16,.69,.83), half3(1,.83,.45), smoothstep(tip-.05,tip,r));
                color = half4(ember * filament,filament) + color * (1-filament);

                // A hot, jagged ignition, followed by a smaller compression flash on a full shot.
                float rays = pow(saturate(.5 + .5 * cos(6 * angle + .55 * sin(3 * angle))), 4);
                float flash = 1 - smoothstep(.025,.23,age);
                float echo = full * saturate(1 - abs(age - .32) / .12);
                float reach = (.48 + .46 * rays) * flash + (.22 + .21 * rays) * echo;
                float spark = (1 - smoothstep(reach - .05,reach,r)) * saturate(flash + echo);
                half3 hot = lerp(half3(1,.67,.30), half3(1,.99,.83), saturate((reach-r)*6));
                color = half4(hot * spark, spark) + color * (1 - spark);
                return color * fade;
            }
            ENDHLSL
        }
    }
}
