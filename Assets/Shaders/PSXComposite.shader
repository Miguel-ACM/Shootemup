// PS1 output stage, kept deliberately light so it does not cover the playfield:
//  * the low internal resolution is chosen by PSXPresentation as an integer division of the
//    screen, so this pass upscales with whole, even pixels
//  * dithering follows the PS1 GPU: a small signed 4x4 offset (-4..+3 on 8-bit values, i.e. half
//    a 5-bit step, matrix as in the DuckStation emulator) added before truncating to 15-bit colour
//  * an optional, mild horizontal blend imitates composite/CRT softness and hides the dither grid
//  * no vignette or colour grading
Shader "Shootemup/PSX Composite"
{
    Properties
    {
        _MainTex("Scene", 2D)="black"{}
        _Color("Tint", Color)=(1,1,1,1)
        _Dither("PS1 dither strength", Range(0,1))=1
        _ColorSteps("Colour steps", Float)=31
        _Soften("Horizontal CRT softness", Range(0,0.5))=0.18
        _Vignette("Vignette (unused)", Range(0,1))=0
        _GlowTex("Projectile radiance",2D)="black"{}
        _GlowStrength("Glow strength",Float)=.6
        _FilterEnabled("Pixel filter",Float)=1
        _Underwater("Underwater grade",Float)=0
        _TimeStop("Stopped time blue filter",Range(0,1))=0
        _ClearFlash("Full clear flash",Range(0,1))=0
        _ClearSweep("Full clear sweep height",Float)=-1
        _ClearTint("Full clear tint",Color)=(1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex,_GlowTex;
            float4 _MainTex_TexelSize, _Color, _ClearTint;
            float _Dither, _ColorSteps, _Soften,_GlowStrength,_FilterEnabled,_Underwater,_TimeStop,_ClearFlash,_ClearSweep;
            struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct Output { float4 position:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            Output vert(Input i) { Output o; o.position=UnityObjectToClipPos(i.vertex); o.uv=i.uv; o.color=i.color*_Color; return o; }
            float3 Surface(float2 uv)
            {
                float3 c=tex2D(_MainTex,uv).rgb;
                // Preserve the projectile hue instead of clipping HDR cores into white columns.
                return c/max(1,max(c.r,max(c.g,c.b)));
            }
            // PS1 GPU dither offsets on the 0..255 scale (DuckStation's table).
            float Offset(float2 p)
            {
                int x=(int)p.x & 3, y=(int)p.y & 3;
                const float values[16]={-4,0,-3,1, 2,-2,3,-1, -3,1,-4,0, 3,-1,2,-2};
                return values[y*4+x];
            }
            half4 frag(Output i):SV_Target
            {
                float2 size=_MainTex_TexelSize.zw;
                float2 pixel=floor(i.uv*size);
                float2 uv=(pixel+0.5)/size;
                float3 color=Surface(uv);
                float3 left=Surface(uv-float2(_MainTex_TexelSize.x,0));
                float3 right=Surface(uv+float2(_MainTex_TexelSize.x,0));
                color=lerp(color,(left+right)*0.5,_Soften*_FilterEnabled);
                // 24-bit -> 15-bit: add the signed offset, then truncate (>> 3) like the GPU.
                float3 c8=saturate(color)*255+Offset(pixel)*_Dither;
                float3 quantized=floor(clamp(c8,0,255)/255*_ColorSteps+0.0001)/_ColorSteps;
                color=lerp(saturate(color),quantized,_FilterEnabled);
                // Add the soft colored spill after quantization to preserve its smooth falloff.
                color+=tex2D(_GlowTex,i.uv).rgb*_GlowStrength;
                color=lerp(color,color*float3(.69,.89,1.04)+float3(.008,.021,.042),_Underwater);
                // Stopped time: a cool monochrome blue wash that still keeps bright rounds readable.
                float luma=dot(color,float3(.299,.587,.114));
                color=lerp(color,luma*float3(.5,.78,1.22)+float3(.015,.045,.11),_TimeStop*.75);
                // Full-screen clear: a white flash and a bright band sweeping up with a soft wake.
                color=lerp(color,float3(1,1,1),_ClearFlash*.45);
                float above=i.uv.y-_ClearSweep;
                float band=_ClearSweep<-.5?0:exp(-above*above*900)+(above<0?.18*exp(above*7):0);
                color+=_ClearTint.rgb*band*.6;
                return half4(color,1)*i.color;
            }
            ENDCG
        }
    }
}
