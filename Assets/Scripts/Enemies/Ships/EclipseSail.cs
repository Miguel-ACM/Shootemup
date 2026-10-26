using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class EclipseSail : DistrictEnemy
{
    protected override Vector2 CueAxis => Vector2.down;
    protected override void Configure()
    {
        hull = 75;
        hullScale = 0.57f;
        health = 53;
        attackInterval = Timing(2.6f, 1.8f);
        nextAttack = 1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        for (int s = -1; s <= 1; s += 2)
            Fire<SimpleRound>(Muzzle(s < 0 ? 0 : 1), CombatVisuals.Rotate(Vector2.down, s * (volley % 2 == 0 ? 34 : 18)), 1.9f, .37f);
        if (!Difficulty.Easy)
            Fan<NeedleRound>(3, 18, 3.9f, .16f);
    }

}

public static partial class WavePartLibrary
{
    static readonly FlightCurve EclipseSailRoute0 = Route("Space / Eclipse orbital crescent", Vector2.right, -1.4f, 0.7f, -0.73f, 0.42f, -0.04f, 0.4f, 0.59f, 0.67f, 0.68f, 1.25f);
    static readonly FlightCurve EclipseSailRoute1 = Route("Space / Eclipse solar arc", Vector2.right, -0.48f, 1.25f, -0.69f, 0.8f, -0.39f, 0.42f, 0.2f, 0.27f, 0.69f, 0.5f, 1.4f, 0.74f);
    static readonly FlightCurve EclipseSailRoute2 = Route("Space / Eclipse limb transit", Vector2.right, 2.2f, 0.73f, 0.7f, 0.82f, 0.12f, 0.72f, -0.32f, 0.41f, -0.68f, 0.12f, -0.72f, -0.4f);
    public static readonly WavePart[] EclipseSailParts =
    {
        new WavePart("Eclipse orbital crescent", nameof(EclipseSail), EclipseSailRoute0, WaveShape.Train, 3, 5.2f, 2, new[] { 0.24f, 0.4f, 0.59f, 0.76f }, new[] { 0.36f, 0.65f }, 3),
        new WavePart("Eclipse solar arc", nameof(EclipseSail), EclipseSailRoute1, WaveShape.Rake, 3, 5.0f, 4, new[] { 0.27f, 0.47f, 0.67f }, new[] { 0.4f, 0.72f }, 3),
        new WavePart("Eclipse limb transit", nameof(EclipseSail), EclipseSailRoute2, WaveShape.Rank, 3, 4.9f, 7, new[] { 0.28f, 0.44f, 0.63f, 0.77f }, new[] { 0.39f, 0.7f }, 3),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedEclipseSail;
    public static Mesh EclipseSailMesh()
    {
        if (cachedEclipseSail != null)
            return cachedEclipseSail;
        var b = new Builder();
        for (int i = 0; i < 8; i++)
        {
            float a = (30 + i * 40) * Mathf.Deg2Rad, n = (30 + (i + 1) * 40) * Mathf.Deg2Rad;
            var p = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
            var q = new Vector3(Mathf.Cos(n), Mathf.Sin(n), 0);
            b.Face(15, p * .56f + Vector3.back * .13f, q * .56f + Vector3.back * .13f, q * .92f + Vector3.back * .13f, p * .92f + Vector3.back * .13f);
            b.Face(13, p * .92f + Vector3.back * .13f, q * .92f + Vector3.back * .13f, q * .92f + Vector3.forward * .13f, p * .92f + Vector3.forward * .13f);
        }
        foreach (int s in new[] { -1, 1 })
        {
            SolarPanel(b, new Vector3(s * .95f, .26f, .05f), .42f, .86f);
            Barrel(b, s * .52f, -.38f, .46f, .16f, -.3f);
        }
        b.Box(new Vector3(0, .55f, 0), new Vector3(.25f, .48f, .35f), 15, 13);
        b.Drum(new Vector3(0, .42f, -.24f), .18f, .14f, 9, 8);
        return cachedEclipseSail = b.Finish("EclipseSail authored hull and weapon lips");
    }
}
