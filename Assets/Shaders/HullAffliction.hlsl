#ifndef SHOOTEMUP_HULL_AFFLICTION
#define SHOOTEMUP_HULL_AFFLICTION

// Measure in world units, relative to the mesh pivot. The pattern rides with a
// moving ship and the same marks stay legible on a scout and a capital hull.
float2 HullSurfacePosition(float3 positionOS)
{
    return mul((float3x3)GetObjectToWorldMatrix(), positionOS).xy;
}

half3 HullAffliction(half3 armor, float2 position, float status)
{
    float2 p = floor(position * 32 + .5) / 32;
    float t = _Time.y;
    half value = dot(armor, half3(.27, .58, .15));
    half3 fire = armor;
    float tongue = 0;
    if (status != 2)
    {
        float bend = sin(p.y * 5.3 - t * 4.4) * .9;
        tongue = .5 + .5 * sin(p.y * 8 - t * 6 + 2.5 * sin(p.x * 9.2 + bend));
        half crest = smoothstep(.60, .94, tongue);
        half3 heat = lerp(half3(.67,.075,.018), half3(1,.56,.095), crest);
        fire = armor * .27 + heat * (.47 + .51 * value + .28 * crest);
    }
    if (status == 1) return fire;

    // Offset successive rows of blisters. The darker interior and moving jade
    // lip read as liquid, even when only a few pixels of a wing are visible.
    float2 cells = p * 2.65;
    cells.y -= t * .52;
    cells.x += .5 * floor(cells.y) + .14 * sin(p.y * 4 + t * 1.7);
    float radius = length(frac(cells) - .5);
    float skin = .29 + .075 * sin(t * 2.4 + dot(floor(cells), float2(1.9,2.7)));
    half rim = 1 - smoothstep(.035, .115, abs(radius - skin));
    half3 jade = lerp(half3(.16,.51,.34), half3(.57,.88,.68), rim);
    half3 poison = armor * .35 + jade * (.55 + .49 * value + .20 * rim);
    if (status == 2) return poison;
    return lerp(poison, fire, smoothstep(.41, .59, tongue));
}
#endif
