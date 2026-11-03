#ifndef SHOOTEMUP_COCKPIT_GLASS
#define SHOOTEMUP_COCKPIT_GLASS

// Pane coordinates run from -1 to 1 across the actual glazing. A shallow curved
// normal gives even the flat hexagonal GR-07 pane a moving reflection as it banks.
float3 CockpitReflection(float2 pane, float3 normalWS, float3 positionOS, float3 paneRightOS, float3 paneUpOS)
{
    float3 eye = GetWorldSpaceNormalizeViewDir(TransformObjectToWorld(positionOS));
    float3 normal = normalize(normalWS);
    normal *= dot(normal, eye) < 0 ? -1 : 1;
    normal = normalize(normal + .40 * (pane.x * TransformObjectToWorldDir(paneRightOS) + pane.y * TransformObjectToWorldDir(paneUpOS)));
    float3 reflected = reflect(-eye, normal);
    return float3(dot(reflected, normalize(float3(-.78,.46,-.24))), length(pane), 1);
}

half3 CockpitGlass(half3 pigment, float3 reflection)
{
    float band = reflection.x;
    half broad = smoothstep(.14,.30,band) * (1 - smoothstep(.48,.63,band));
    half glint = smoothstep(.22,.29,band) * (1 - smoothstep(.32,.40,band));
    half rim = smoothstep(.62,1.05,reflection.y);
    half3 deep = pigment * .68 + half3(.008,.013,.023);
    half3 glass = lerp(deep, pigment * .68 + half3(.23,.34,.39), broad * .76);
    glass += half3(.035,.067,.076) * rim;
    return lerp(glass,half3(.93,.98,1),glint * .78);
}
#endif
