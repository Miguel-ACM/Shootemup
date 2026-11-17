Shader "Shootemup/Soft Orbital Sky"
{
    Properties
    {
        [NoScaleOffset] _Atmospheres("Emission, dust, cloud belts and crater relief", 2D) = "gray" {}
        _Drift("Stellar drift", Vector) = (0,0,0,0)
        _Seed("Sky seed", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Background" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Cull Off ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_Atmospheres); SAMPLER(sampler_Atmospheres);
            CBUFFER_START(UnityPerMaterial)
            float4 _Drift;
            float _Seed;
            CBUFFER_END

            struct Input { float4 positionOS : POSITION; };
            struct Output { float4 positionCS : SV_POSITION; float2 p : TEXCOORD0; };
            Output Vert(Input v)
            {
                Output o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.p = v.positionOS.xy;
                return o;
            }

            float Hash(float2 p)
            {
                float3 q = frac(float3(p.xyx) * .1031 + _Seed * .013);
                q += dot(q, q.yzx + 33.33);
                return frac((q.x + q.y) * q.z);
            }

            float Stars(float2 p, float scale, float drift)
            {
                float2 grid = (p + _Drift.xy * drift) * scale;
                float2 cell = floor(grid);
                float2 delta = frac(grid) - (.17 + .66 * float2(Hash(cell + 3.7), Hash(cell + 21.1)));
                float r = Hash(cell);
                float radius = lerp(.019, .055, smoothstep(.84,1,r));
                float aa = max(.007, .58 * fwidth(grid.x));
                return (1 - smoothstep(max(0, radius-aa), radius+aa, length(delta)))
                    * step(.69,r) * lerp(.18,.51,r);
            }

            static const float3 Sun = float3(.69,.57,.445);
            // Coordinates on the ring plane: its near half passes in front of the globe.
            static const float3 RingNormal = float3(-.263,.899,.350);
            float RingBands(float r)
            {
                float main = smoothstep(1.23,1.30,r) * (1 - smoothstep(1.81,1.86,r));
                float division = smoothstep(.012,.028,abs(r-1.655));
                float outer = smoothstep(1.90,1.93,r) * (1 - smoothstep(2.04,2.08,r));
                return main * division * (.76 + .14*cos(r*83) + .09*cos(r*157)) + .36*outer;
            }

            float3 Giant(float3 sky, float2 q, float time)
            {
                float d = length(q);
                float aa = max(.003, fwidth(q.x));
                if (d > 2.10 + aa) return sky;
                float z = sqrt(saturate(1-dot(q,q)));
                float3 n = float3(q,z);
                float light = dot(n,Sun);
                float sunward = smoothstep(-.32,.76,light);
                float latitude = dot(n,RingNormal);
                float longitude = atan2(q.x+.0001,z) * .159155;
                float bands = SAMPLE_TEXTURE2D(_Atmospheres,sampler_Atmospheres,
                    float2(.50+longitude+time*.00075,.5+asin(clamp(latitude,-.999,.999))*.31831)).b;
                float3 deck = lerp(float3(.105,.205,.236),float3(.38,.48,.47),smoothstep(.18,.77,bands));
                deck = lerp(deck,float3(.52,.485,.375),.62*smoothstep(.69,.91,bands));
                float3 globe = deck * (.15+.85*sunward);
                float shadowDistance = -latitude / dot(Sun,RingNormal);
                float shadow = RingBands(length(n+Sun*shadowDistance)) * step(.0,shadowDistance);
                globe *= 1 - shadow*.38;
                float rim = pow(saturate(1-z),3);
                globe += float3(.09,.19,.22)*rim*sunward;
                float air = (1-smoothstep(1.0,1.065,d)) * smoothstep(-.40,.8,dot(q,Sun.xy));
                sky += float3(.044,.109,.14)*air;
                float cover = 1 - smoothstep(1-aa,1+aa,d);
                sky = lerp(sky,globe,cover);

                float ringZ = -dot(q,RingNormal.xy) / RingNormal.z;
                float3 ringPoint = float3(q,ringZ);
                float r = length(ringPoint);
                float toSun = dot(ringPoint,Sun);
                float shadowCone = length(ringPoint-Sun*toSun);
                float sunlight = lerp(.27,1, max(step(0,toSun),smoothstep(.91,1.09,shadowCone)));
                float ring = RingBands(r) * (1-cover*(1-smoothstep(-aa,aa,ringZ-z)));
                float3 dust = lerp(float3(.24,.29,.31),float3(.52,.44,.31),smoothstep(1.32,1.84,r));
                return lerp(sky,dust*sunlight,.82*ring);
            }

            float3 Moon(float3 sky,float2 q)
            {
                float aa = max(.005,fwidth(q.x));
                if (dot(q,q)>1.08) return sky;
                float z = sqrt(saturate(1-dot(q,q)));
                float3 n = float3(q,z);
                float relief = SAMPLE_TEXTURE2D(_Atmospheres,sampler_Atmospheres,
                    float2(atan2(q.x+.0001,z)*.27+.54,asin(clamp(q.y,-.999,.999))*.35+.44)).a;
                float light = smoothstep(-.08,.74,dot(n,Sun));
                float3 rock = lerp(float3(.085,.084,.12),float3(.32,.30,.34),relief)
                    * (.22 + .86*light);
                return lerp(sky,rock,1-smoothstep(1-aa,1+aa,length(q)));
            }

            half4 Frag(Output i) : SV_Target
            {
                float2 p = i.p;
                float seed = frac(_Seed*.618034);
                float2 far = p + _Drift.xy*.17;
                float2 uv = far * float2(.046,.032) + float2(seed,seed*.73);
                float2 cloud = SAMPLE_TEXTURE2D(_Atmospheres,sampler_Atmospheres,uv).rg;
                float lane = p.x*.83-p.y*.45-1.2+1.12*sin(far.y*.27+seed*6.28)+.70*sin(_Drift.z*.019);
                float band = exp2(-.11*lane*lane);
                float density = smoothstep(.23,.90,cloud.r)*band;
                float darkDust = smoothstep(.39,.68,cloud.g);
                float3 haze = lerp(float3(.106,.092,.173),float3(.104,.225,.254),
                    smoothstep(.29,.73,cloud.g));
                float3 sky = float3(.017,.024,.054) + haze*density*(1-.52*darkDust);
                sky += float3(.068,.087,.135)*density*density*(1-darkDust);
                float fine = Stars(p+17,2.9,.32) * (.40+.60*band);
                sky += fine * float3(.58,.74,.88) + Stars(p,1.31,.93)*float3(.91,.88,.83);
                // Field scroll is continuous. Distant bodies drift inside the composition.
                float time = _Drift.z;
                float2 shift = float2(_Drift.x*.46+.37*sin(time*.031+seed*6.28),
                    .58*sin(time*.026+seed*3.14));
                float2 giant = float2(-3.48-.65*seed,3.63+1.18*seed) + shift;
                sky = Giant(sky,(p-giant)/2.91,time);
                float2 moon = float2(3.18+.61*seed,-3.72+.83*seed) + shift*.58;
                sky = Moon(sky,(p-moon)/.92);
                return half4(sky,1);
            }
            ENDHLSL
        }
    }
}
