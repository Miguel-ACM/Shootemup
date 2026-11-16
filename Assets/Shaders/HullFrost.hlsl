#ifndef SHOOTEMUP_HULL_FROST
#define SHOOTEMUP_HULL_FROST

half3 HullFrost(half3 armor, float2 p)
{
    float2 lattice = float2(p.x + .56 * p.y, p.y) * 6;
    float2 crystal = abs(frac(lattice + .5) - .5);
    float growth = HullNoise(p * 4.1) * .7 + HullNoise(p * 12 + 1.9) * .3;
    half glaze = smoothstep(.27, .66, growth);
    half ridge = 1 - smoothstep(.018, .073, abs(crystal.x + crystal.y * .55 - .24));
    half branch = 1 - smoothstep(.018, .064, min(crystal.x, crystal.y));
    half facets = max(ridge, branch * .62) * smoothstep(.48, .72, growth);
    half3 ice = armor * half3(.50, .75, .94) + half3(.12, .34, .43) * glaze;
    return lerp(armor, ice + half3(.37, .60, .66) * facets, .25 + glaze * .55);
}

#endif
