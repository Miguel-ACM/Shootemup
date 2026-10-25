Shader "Shootemup/PSX Valley"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            ZWrite On Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 _PSXSnapResolution;
            struct Input { float4 positionOS:POSITION; float3 normalOS:NORMAL; float4 color:COLOR; float2 uv:TEXCOORD0; float2 surface:TEXCOORD1; };
            struct Output { float4 positionCS:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float meadow:TEXCOORD1; };
            Output Vert(Input i)
            {
                Output o;
                o.positionCS=TransformObjectToHClip(i.positionOS.xyz);
                float2 grid=max(_PSXSnapResolution.xy*.5,1);
                float2 ndc=o.positionCS.xy/o.positionCS.w;
                o.positionCS.xy=floor(ndc*grid+.5)/grid*o.positionCS.w;
                float3 n=TransformObjectToWorldNormal(i.normalOS);
                float light=.72+.28*floor(saturate(dot(n,normalize(float3(-.4,.65,-.8))))*3+.5)/3;
                o.color=float4(i.color.rgb*light,1);
                o.uv=i.uv;o.meadow=i.surface.x;return o;
            }
            float Hash(float2 p,float period)
            {
                p.y=p.y-floor(p.y/period)*period;
                return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);
            }
            float Noise(float2 p,float period)
            {
                float2 cell=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(Hash(cell,period),Hash(cell+float2(1,0),period),f.x),
                    lerp(Hash(cell+float2(0,1),period),Hash(cell+1,period),f.x),f.y);
            }
            half4 Frag(Output i):SV_Target
            {
                // Quiet, low contrast texels; detail comes primarily from the authored shapes.
                float2 p=floor(i.uv*12);
                p.y=p.y-floor(p.y/1152)*1152;
                float grain=frac(sin(dot(p,float2(12.9898,78.233)))*43758.5453);
                float3 c=i.color.rgb*(.985+grain*.03);
                if(i.meadow>.5)
                {
                    // World-locked patches of short grass, with a few exposed earth flecks.
                    // Each octave repeats exactly over the 96-unit scrolling landscape.
                    float2 ground=floor(i.uv*32)/32;
                    float clump=Noise(ground*.5,48),mottle=Noise(ground*2,192);
                    c*=.91+.15*clump+.10*mottle;
                    c=lerp(c,c*float3(1.05,.92,.78),smoothstep(.70,.87,mottle)*.36);
                    float2 cell=floor(ground*4),f=frac(ground*4);
                    float seed=Hash(cell,384);
                    float2 root=float2(.25+.5*Hash(cell+17,384),.12+.20*seed);
                    float height=.36+.35*Hash(cell+53,384);
                    float t=(f.y-root.y)/height;
                    float blade=0;
                    // Uneven little fans of three tapered blades, never a checkerboard.
                    for(int n=-1;n<=1;n++)
                    {
                        float bend=(n*.20+(seed-.5)*.24)*t;
                        float w=.07*(1-saturate(t))+.015;
                        blade=max(blade,(1-smoothstep(w,w+.035,abs(f.x-root.x-bend)))*step(0,t)*step(t,1));
                    }
                    float density=smoothstep(.20,.65,clump)*step(.22,seed);
                    c+=float3(.036,.057,.017)*blade*density;
                    c-=float3(.018,.026,.010)*blade*(1-density)*.5;
                }
                return half4(c,1);
            }
            ENDHLSL
        }
    }
}
