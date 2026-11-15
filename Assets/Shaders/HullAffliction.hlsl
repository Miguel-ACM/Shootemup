#ifndef SHOOTEMUP_HULL_AFFLICTION
#define SHOOTEMUP_HULL_AFFLICTION

// Measure in world units, relative to the mesh pivot. The pattern rides with a
// moving ship and the same marks stay legible on a scout and a capital hull.
float2 HullSurfacePosition(float3 positionOS)
{
    return mul((float3x3)GetObjectToWorldMatrix(), positionOS).xy;
}

float HullHash(float2 p)
{
    p = frac(p * float2(123.34, 345.45));
    p += dot(p, p + 34.345);
    return frac(p.x * p.y);
}

float HullNoise(float2 p)
{
    float2 c = floor(p), f = frac(p);
    f = f * f * (3 - 2 * f);
    return lerp(lerp(HullHash(c), HullHash(c + float2(1, 0)), f.x), lerp(HullHash(c + float2(0, 1)), HullHash(c + 1), f.x), f.y);
}

// status is a bit set: 1 burning, 2 poisoned. Decoded as integers so one coating never
// leaks into the other.
half3 HullAffliction(half3 armor, float2 position, float status)
{
    int bits = (int)(status + .5);
    bool burning = (bits & 1) != 0, toxic = (bits & 2) != 0;
    float2 p = floor(position * 32 + .5) / 32;
    float t = _Time.y;
    half value = dot(armor, half3(.27, .58, .15));
    half3 fire = armor;
    float tongue = 0;
    if (burning)
    {
        float bend = sin(p.y * 5.3 - t * 4.4) * .9;
        tongue = .5 + .5 * sin(p.y * 8 - t * 6 + 2.5 * sin(p.x * 9.2 + bend));
        half crest = smoothstep(.60, .94, tongue);
        half3 heat = lerp(half3(.67,.075,.018), half3(1,.56,.095), crest);
        fire = armor * .27 + heat * (.47 + .51 * value + .28 * crest);
    }
    if (!toxic) return fire;

    // Poison (Miguel, 15 November 2026: the jade blister grid was rejected). The hull keeps its
    // own paint, turned a little sickly, while dark corrosion stains creep across it and swell
    // and ebb, each edged with a glowing jade lip, and faint veins pulse between them.
    float2 drift = float2(.07, -.11) * t;
    float n = HullNoise(p * 2.3 + drift) * .65 + HullNoise(p * 5.1 - drift * 1.6 + 3.7) * .35;
    float creep = .56 + .05 * sin(t * 1.3);
    half stain = step(creep, n);
    half lip = step(creep - .045, n) - stain;
    float vein = 1 - smoothstep(0, .03, abs(HullNoise(p * 3.4 + 9.1 + drift * .5) - .5));
    float wave = .5 + .5 * sin(p.y * 6 + p.x * 2 + t * 3.2);
    half grey = dot(armor, half3(.3, .59, .11));
    half3 sick = lerp(armor, grey * half3(.78, 1.04, .9), .32) * half3(.88, 1, .92);
    half3 rot = half3(.06, .19, .15) + armor * .22;
    half3 glow = half3(.47, .87, .66);
    half3 poison = lerp(sick, rot, stain);
    poison = lerp(poison, glow, lip * .85);
    poison = lerp(poison, glow, vein * (1 - stain) * (.25 + .45 * wave));
    if (!burning) return poison;
    return lerp(poison, fire, smoothstep(.41, .59, tongue));
}
#endif
