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
            sampler2D _MainTex;
            float4 _MainTex_TexelSize, _Color;
            float _Dither, _ColorSteps, _Soften;
            struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct Output { float4 position:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            Output vert(Input i) { Output o; o.position=UnityObjectToClipPos(i.vertex); o.uv=i.uv; o.color=i.color*_Color; return o; }
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
                float3 color=tex2D(_MainTex,uv).rgb;
                float3 left=tex2D(_MainTex,uv-float2(_MainTex_TexelSize.x,0)).rgb;
                float3 right=tex2D(_MainTex,uv+float2(_MainTex_TexelSize.x,0)).rgb;
                color=lerp(color,(left+right)*0.5,_Soften);
                // 24-bit -> 15-bit: add the signed offset, then truncate (>> 3) like the GPU.
                float3 c8=saturate(color)*255+Offset(pixel)*_Dither;
                color=floor(clamp(c8,0,255)/255*_ColorSteps+0.0001)/_ColorSteps;
                return half4(color,1)*i.color;
            }
            ENDCG
        }
    }
}
