Shader "Shootemup/Solid Projectile"
{
    Properties { _Tint("Tint",Color)=(1,1,1,1) _Stretch("Collider scale",Vector)=(1,1,1,0) _Burn("Burn coating",Float)=0 _Poison("Poison coating",Float)=0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+110" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            UNITY_INSTANCING_BUFFER_START(Projectile)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Tint)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Stretch)
                UNITY_DEFINE_INSTANCED_PROP(float, _Burn)
                UNITY_DEFINE_INSTANCED_PROP(float, _Poison)
            UNITY_INSTANCING_BUFFER_END(Projectile)
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; half4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; half4 color:COLOR; };
            Varyings Vert(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                Varyings o;
                float3 stretch=UNITY_ACCESS_INSTANCED_PROP(Projectile,_Stretch).xyz;
                o.positionCS=TransformObjectToHClip(v.positionOS.xyz*stretch);
                float3 normal=TransformObjectToWorldNormal(v.normalOS/stretch);
                float light=.44+.56*saturate(dot(normal,normalize(float3(-.45,.65,-.8))));
                o.color=v.color*UNITY_ACCESS_INSTANCED_PROP(Projectile,_Tint);
                float burn=UNITY_ACCESS_INSTANCED_PROP(Projectile,_Burn);
                float brightness=max(o.color.r,max(o.color.g,o.color.b));
                o.color.rgb=lerp(o.color.rgb,float3(1,.15,.07)*brightness,burn);
                float poison=UNITY_ACCESS_INSTANCED_PROP(Projectile,_Poison);
                // Both statuses retain their own colour: red nose, green tail.
                float toxic=poison*(1-burn*step(0,v.positionOS.y));
                o.color.rgb=lerp(o.color.rgb,float3(.22,1,.2)*brightness,toxic);
                o.color.rgb*=light;
                return o;
            }
            half4 Frag(Varyings v):SV_Target { return v.color; }
            ENDHLSL
        }
    }
}
