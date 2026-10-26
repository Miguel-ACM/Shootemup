using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class DuneMoth : DistrictEnemy
{
    protected override Vector2 CueAxis => Vector2.down;
    protected override void Configure()
    {
        hull = 71;
        hullScale = 0.53f;
        health = 32;
        attackInterval = Timing(2.75f, 1.95f);
        nextAttack = 1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        for (int s = -1; s <= 1; s += 2)
            for (int j = 0; j < Shots(2, 3); j++)
                Fire<SimpleRound>(Muzzle(s < 0 ? 0 : 1), CombatVisuals.Rotate(Vector2.down, s * (17 + j * 18)), 2.5f + j * .2f, .24f);
    }

}

public static partial class WavePartLibrary
{
    static readonly FlightCurve DuneMothRoute0 = Route("Desert / Moth dune scallop", Vector2.right, -1.4f, 0.84f, -0.67f, 0.64f, -0.16f, 0.4f, 0.32f, 0.47f, 0.7f, 0.75f, 0.68f, 1.25f);
    static readonly FlightCurve DuneMothRoute1 = Route("Desert / Moth slipface rake", Vector2.right, -0.68f, 1.25f, -0.51f, 0.85f, 0.1f, 0.65f, 0.63f, 0.34f, 0.78f, -0.25f);
    static readonly FlightCurve DuneMothRoute2 = Route("Desert / Moth leeward rise", Vector2.right, 1.4f, 0.32f, 0.7f, 0.31f, 0.12f, 0.42f, -0.37f, 0.67f, -0.61f, 1.25f);
    public static readonly WavePart[] DuneMothParts =
    {
        new WavePart("Moth dune scallop", nameof(DuneMoth), DuneMothRoute0, WaveShape.Train, 4, 4.8f, 2, new[] { 0.29f, 0.48f, 0.72f }, new[] { 0.4f, 0.72f }, 3),
        new WavePart("Moth slipface rake", nameof(DuneMoth), DuneMothRoute1, WaveShape.Rake, 3, 4.8f, 4, new[] { 0.27f, 0.53f, 0.74f }, new[] { 0.39f, 0.74f }, 3),
        new WavePart("Moth leeward rise", nameof(DuneMoth), DuneMothRoute2, WaveShape.Chevron, 3, 4.9f, 7, new[] { 0.32f, 0.62f }, new[] { 0.48f }, 3),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedDuneMoth;
    public static Mesh DuneMothMesh()
    {
        if (cachedDuneMoth != null)
            return cachedDuneMoth;
        var b = new Builder();
        BevelArmor(b, new[] { V(0, -.83f), V(-.16f, -.2f), V(-.18f, .71f), V(.18f, .71f), V(.16f, -.2f) }, .28f, 13);
        foreach (int s in new[] { -1, 1 })
        {
            Armor(b, new[] { V(s * .14f, .42f), V(s * 1.2f, .67f), V(s * .92f, -.27f), V(s * .43f, -.08f) }, .1f, 15);
            b.Prism(new[] { V(s * .27f, .35f), V(s * .98f, .48f), V(s * .71f, -.12f) }, .07f, 13, Vector3.back * .13f, 6);
            Barrel(b, s * .72f, -.1f, .34f, .14f, -.16f);
            Motor(b, s * .29f, .68f, .18f, .29f, 13);
        }
        Glass(b, 0, -.12f, .2f, .35f, -.28f);
        return cachedDuneMoth = b.Finish("DuneMoth authored hull and weapon lips");
    }
}
