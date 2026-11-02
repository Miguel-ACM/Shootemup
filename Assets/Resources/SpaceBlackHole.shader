Shader "Shootemup/Space black hole"
{
    Properties
    {
        _Tint("Tint", Color) = (1,1,1,1)
        _Extent("Extent", Vector) = (3,2,0,0)
        _CoreOnly("Horizon only", Float) = 0
        _HitFlash("Hit", Float) = 0
        _Feeding("Accretion", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
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
            float4 _Tint, _Extent;
            float _CoreOnly, _HitFlash, _Feeding;
            CBUFFER_END
            struct Input { float4 p:POSITION; float2 uv:TEXCOORD0; };
            struct Output { float4 p:SV_POSITION; float2 uv:TEXCOORD0; };
            Output Vert(Input i) { Output o; o.p=TransformObjectToHClip(i.p.xyz); o.uv=i.uv; return o; }
            half4 Frag(Output i):SV_Target
            {
                float2 p = (i.uv*2-1)*_Extent.xy;
                p = floor(p*64+.5)/64;
                float r=length(p), horizon=1-smoothstep(.745,.77,r);
                if (_CoreOnly>.5)
                {
                    float edge=smoothstep(.63,.74,r);
                    return half4(lerp(float3(.001,.002,.004),float3(.8,.5,.2),edge*_HitFlash),horizon*_Tint.a);
                }
                float t=_Time.y*.32;
                float diskRadius=length(float2(p.x,p.y/.23));
                float angle=atan2(p.y/.23,p.x);
                float disk=smoothstep(.85,1.08,diskRadius)*(1-smoothstep(2.05,2.85,diskRadius));
                float filaments=.54+.23*sin(diskRadius*43-angle*3+t*8)+.13*sin(diskRadius*91+angle*7-t*11);
                float doppler=.68+.32*saturate(-p.x/2+.5);
                float band=disk*saturate(filaments)*doppler;
                // The back of the disk is lensed over and below the dark silhouette.
                float arcRadius=length(float2(p.x,p.y*.93));
                float arcs=exp(-abs(arcRadius-1.01)*22)*(p.y>0 ? .9 : .38);
                arcs*=.65+.35*sin(atan2(p.y,p.x)*9-t*3);
                float photon=exp(-abs(r-.805)*70);
                float glow=exp(-abs(r-.84)*9)*.15;
                float light=saturate(band+arcs*.66+photon*.92+glow);
                float3 warm=lerp(float3(.48,.13,.035),float3(1,.67,.28),saturate(light*1.5));
                warm=lerp(warm,float3(1,.91,.67),saturate((light-.65)*2.6));
                float alpha=saturate(disk*.9+arcs+photon+glow);
                float3 color=warm*(.75+_Feeding*.12);
                color=lerp(color,float3(.001,.002,.004),horizon);
                alpha=max(alpha,horizon);
                return half4(color*_Tint.rgb,alpha*_Tint.a);
            }
            ENDHLSL
        }
    }
}
