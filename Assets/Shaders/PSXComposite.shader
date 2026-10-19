Shader "Shootemup/PSX Composite"
{
    Properties
    {
        _MainTex("Scene", 2D)="black"{}
        _Color("Tint", Color)=(1,1,1,1)
        _Dither("Ordered dithering", Range(0,1))=0.65
        _ColorSteps("Colour steps", Float)=31
        _Vignette("Vignette", Range(0,1))=0.20
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
            float _Dither, _ColorSteps, _Vignette;
            struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct Output { float4 position:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            Output vert(Input i) { Output o; o.position=UnityObjectToClipPos(i.vertex); o.uv=i.uv; o.color=i.color*_Color; return o; }
            float Bayer(float2 p)
            {
                int x=(int)p.x & 3, y=(int)p.y & 3;
                const float values[16]={0,8,2,10,12,4,14,6,3,11,1,9,15,7,13,5};
                return (values[y*4+x]+0.5)/16.0-0.5;
            }
            half4 frag(Output i):SV_Target
            {
                float2 size=_MainTex_TexelSize.zw;
                float2 pixel=floor(i.uv*size);
                float2 uv=(pixel+0.5)/size;
                float3 color=tex2D(_MainTex,uv).rgb;
                color=saturate((color-0.06)*1.10+0.06);
                float lum=dot(color,float3(0.299,0.587,0.114));
                color=lerp(color,lum*float3(0.93,1.0,1.02),0.12);
                color=floor(saturate(color+Bayer(pixel)*_Dither/_ColorSteps)*_ColorSteps+0.5)/_ColorSteps;
                float2 edge=i.uv*2-1;
                color*=1-_Vignette*pow(saturate(dot(edge,edge)*0.48),2);
                return half4(color,1)*i.color;
            }
            ENDCG
        }
    }
}
