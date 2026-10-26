using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class NautilusBell : DistrictEnemy
{
    protected override Vector2 CueAxis => Vector2.down;
    protected override void Configure()
    {
        hull = 77;
        hullScale = 0.57f;
        health = 43;
        attackInterval = Timing(3.5f, 2.55f);
        nextAttack = 1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        int n = Shots(3, 5);
        for (int i = 0; i < n; i++)
            Fire<SimpleRound>(Muzzle(i), CombatVisuals.Rotate(Vector2.down, (i - (n - 1) * .5f) * 29 + (volley % 2 == 0 ? 9 : -9)), 1.9f, .32f);
    }

}

public static partial class WavePartLibrary
{
    static readonly FlightCurve NautilusBellRoute0 = Route("Underwater / Bell buoyant procession", Vector2.right, -0.65f, -0.25f, -0.62f, 0.18f, -0.31f, 0.46f, 0.18f, 0.67f, 0.58f, 0.91f, 0.62f, 1.25f);
    static readonly FlightCurve NautilusBellRoute1 = Route("Underwater / Bell tidal arch", Vector2.right, -1.4f, 0.41f, -0.72f, 0.61f, -0.12f, 0.77f, 0.49f, 0.71f, 0.8f, 0.45f, 1.4f, 0.3f);
    static readonly FlightCurve NautilusBellRoute2 = Route("Underwater / Bell reefward roll", Vector2.right, 0.63f, 1.25f, 0.51f, 0.85f, 0.08f, 0.61f, -0.49f, 0.4f, -0.69f, 0.08f, -0.72f, -0.25f);
    public static readonly WavePart[] NautilusBellParts =
    {
        new WavePart("Bell buoyant procession", nameof(NautilusBell), NautilusBellRoute0, WaveShape.Train, 3, 5.7f, 2, new[] { 0.34f, 0.68f }, new[] { 0.5f }, 2),
        new WavePart("Bell tidal arch", nameof(NautilusBell), NautilusBellRoute1, WaveShape.Rank, 2, 5.5f, 4, new[] { 0.32f, 0.66f }, new[] { 0.48f }, 2),
        new WavePart("Bell reefward roll", nameof(NautilusBell), NautilusBellRoute2, WaveShape.Rake, 2, 5.4f, 7, new[] { 0.3f, 0.65f }, new[] { 0.47f }, 2),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedNautilusBell;
    public static Mesh NautilusBellMesh()
    {
        if (cachedNautilusBell != null)
            return cachedNautilusBell;
        var b = new Builder();
        for (int i = 0; i < 11; i++)
        {
            float a = i * .61f, r = .18f + i * .052f;
            var p = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, -.05f - i * .008f);
            b.Drum(p, .18f + i * .006f, .22f, 13, 6, 6);
        }
        b.Drum(new Vector3(0, .02f, -.3f), .2f, .1f, 9, 8);
        foreach (int s in new[] { -1, 1 })
        {
            b.Prism(new[] { V(s * .35f, -.12f), V(s * .7f, -.33f), V(s * .58f, -.68f), V(s * .32f, -.53f) }, .2f, 15, Vector3.zero, 6);
            Barrel(b, s * .44f, -.3f, .42f, .16f, -.23f);
        }
        return cachedNautilusBell = b.Finish("NautilusBell authored hull and weapon lips");
    }
}
