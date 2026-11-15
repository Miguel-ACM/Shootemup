Shader "Shootemup/Solar Fire"
{
    // A wall of solar fire filling one half of the arena. Object space: x runs from the outer screen
    // edge (0) inward to the dividing line (_Boundary); y is world height. _Fill advances the front
    // from the edge to the line; _Heat brightens the warning glow before the eruption; _Fade clears it.
    Properties
    {
        _Boundary("Distance to the dividing line",Float)=5
        _Fill("Front progress",Range(0,1))=0
        _Heat("Warning heat",Range(0,1))=0
        _Warn("Warning tint",Range(0,1))=0
        _Fade("Fade",Range(0,1))=1
        _Clock("Clock",Float)=0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Blend One OneMinusSrcAlpha
            ColorMask RGB
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _Boundary,_Fill,_Heat,_Warn,_Fade,_Clock;
            CBUFFER_END
            struct I { float4 p:POSITION; };
            struct O { float4 p:SV_POSITION; float2 xy:TEXCOORD0; };
            O Vert(I i) { O o; o.p=TransformObjectToHClip(i.p.xyz); o.xy=i.p.xy; return o; }
            float Hash(float2 p) { p=frac(p*float2(123.34,345.45)); p+=dot(p,p+34.345); return frac(p.x*p.y); }
            float Noise(float2 p)
            {
                float2 c=floor(p),f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(Hash(c),Hash(c+float2(1,0)),f.x),lerp(Hash(c+float2(0,1)),Hash(c+1),f.x),f.y);
            }
            float Fbm(float2 p) { return Noise(p)*.5+Noise(p*2.03+3.1)*.27+Noise(p*4.07+7.7)*.15+Noise(p*8.1+1.3)*.08; }
            half4 Frag(O i):SV_Target
            {
                float d=i.xy.x, y=i.xy.y, t=_Clock;
                // Nothing crosses the dividing line: the fire stops on it with a short feather.
                float edge=saturate((_Boundary-d)*8);
                // Flames roll inward and rise; tongues lick ahead of the advancing front.
                float2 flow=float2(d*1.3-t*1.9, y*.6-t*2.6);
                float turb=Fbm(flow+Fbm(flow*.6+float2(t*.3,-t*.5))*1.5);
                float tongues=(Fbm(float2(y*1.3-t*3.4,t*.8))-.35)*1.4;
                float reach=_Fill*(_Boundary+1)-.6+tongues*.6;
                float body=saturate((reach-d)*2.4+turb*.7-.15)*edge;
                float lick=pow(saturate(turb*1.3-.12),1.5);
                // Sparks rising through the fire.
                float2 cell=float2(d*2.6,y*2.2-t*5.5), id=floor(cell), f=frac(cell)-.5;
                float2 at=(float2(Hash(id+7),Hash(id+3))-.5)*.6;
                float spark=step(.9,Hash(id))*saturate(1-length(f-at)*7)*saturate((reach-d)*1.5+.3)*edge;
                // Warning: a breathing glow from the outer edge.
                float glow=_Heat*exp(-d*.8)*(.6+.4*sin(t*9+y*.8));
                // Deep red through orange and yellow to white-hot cores.
                float heat=saturate(body*(.2+.95*lick)+glow*.5);
                float3 colour=lerp(float3(.4,.04,.02),float3(.95,.22,.04),saturate(heat/.35));
                colour=lerp(colour,float3(1,.52,.1),saturate((heat-.35)/.3));
                colour=lerp(colour,float3(1,.86,.36),saturate((heat-.65)/.22));
                colour=lerp(colour,float3(1,.98,.82),saturate((heat-.88)/.12));
                // A hot seam rides the advancing front.
                float seam=exp(-pow((d-reach)*3.2,2))*saturate(_Fill*3)*edge;
                colour+=float3(1,.6,.2)*seam*.6;
                colour=lerp(colour,float3(1,.95,.7),spark);
                float fire=saturate(max(body*(.8+.2*lick),glow*.8)+seam*.4+spark);
                // While it warns, the whole doomed half pulses a warm tint.
                float tint=_Warn*.25*(.7+.3*sin(t*14))*edge;
                float alpha=fire+tint*(1-fire);
                float3 premultiplied=colour*fire+float3(1,.32,.08)*tint*(1-fire);
                return half4(premultiplied*_Fade,alpha*_Fade);
            }
            ENDHLSL
        }
    }
}
