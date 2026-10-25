Shader "Shootemup/Projectile Glow"
{
    Properties
    {
        _MainTex("HDR source",2D)="black"{}
        _Direction("Blur direction",Vector)=(0,0,0,0)
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex;
        float4 _MainTex_TexelSize,_Direction;
        half3 Bright(float2 uv)
        {
            half3 c=tex2D(_MainTex,uv).rgb;
            half peak=max(c.r,max(c.g,c.b));
            return c*(max(0,peak-1.05)/max(peak,.001));
        }
        ENDCG
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Extract
            half4 Extract(v2f_img i):SV_Target
            {
                float2 d=_MainTex_TexelSize.xy;
                return half4((Bright(i.uv+d)+Bright(i.uv-d)+Bright(i.uv+float2(d.x,-d.y))+Bright(i.uv+float2(-d.x,d.y)))*.25,1);
            }
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Blur
            half4 Blur(v2f_img i):SV_Target
            {
                half3 c=tex2D(_MainTex,i.uv).rgb*.227027;
                c+=(tex2D(_MainTex,i.uv+_Direction.xy*1.384615).rgb+tex2D(_MainTex,i.uv-_Direction.xy*1.384615).rgb)*.316216;
                c+=(tex2D(_MainTex,i.uv+_Direction.xy*3.230769).rgb+tex2D(_MainTex,i.uv-_Direction.xy*3.230769).rgb)*.070270;
                return half4(c,1);
            }
            ENDCG
        }
    }
}
