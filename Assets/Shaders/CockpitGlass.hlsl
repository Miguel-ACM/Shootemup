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

// The pilot's pane keeps a bright lit body between reflections, with a wide sheen and a hot
// specular core that slide across it as the ship banks, and a dark glass edge.
// This is unlit colour in the existing pass: no extra light, bloom or geometry.
half3 PilotCockpitGlass(half3 pigment, float3 reflection)
{
    half3 lit = lerp(pigment, half3(1,.98,.92), .46);
    half3 glass = max(lit, CockpitGlass(pigment, reflection));
    float band = reflection.x;
    half sheen = smoothstep(.06,.26,band) * (1 - smoothstep(.44,.70,band));
    half core = smoothstep(.19,.26,band) * (1 - smoothstep(.30,.39,band));
    glass = lerp(glass, half3(.96,.99,1), saturate(sheen * .35 + core * .9));
    half edge = smoothstep(.84,1.08,reflection.y);
    return lerp(glass, pigment * .45 + half3(.07,.08,.09), edge * .5);
}

// The player's pane bypasses the PSX filter. Its fragments leave this alpha in the scene, which
// a later round or effect over the pane overwrites, so PSXComposite only swaps in the sharp,
// full-resolution pane (PSXPresentation's cockpit camera) where nothing was drawn over it.
#define PILOT_PANE_MARK .996
// Set only while the cockpit camera renders: player hulls then draw nothing but the pane.
float _CockpitPass;
#endif
