using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class BaffleSpinner : DistrictEnemy
{
    protected override Vector2 CueAxis => Vector2.down;
    protected override void Configure()
    {
        hull = 67;
        hullScale = 0.48f;
        health = 30;
        attackInterval = (2.05f);
        nextAttack = 1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        for (int s = -1; s <= 1; s += 2)
            Fire<ReturningDisc>(Muzzle(s < 0 ? 0 : 1), CombatVisuals.Rotate(Vector2.down, s * (volley % 2 == 0 ? 35 : 55)), 2.6f, .31f);
    }

}

public static partial class WavePartLibrary
{
    static readonly FlightCurve BaffleSpinnerRoute0 = Route("Tunnel / Baffle wall rebound", Vector2.right, -0.84f, 1.25f, -0.86f, 0.8f, -0.5f, 0.57f, 0.16f, 0.65f, 0.78f, 0.85f, 1.4f, 0.91f);
    static readonly FlightCurve BaffleSpinnerRoute1 = Route("Tunnel / Baffle reflected rank", Vector2.right, -1.4f, 0.39f, -0.76f, 0.46f, -0.42f, 0.7f, -0.69f, 0.94f, -0.83f, 1.25f);
    static readonly FlightCurve BaffleSpinnerRoute2 = Route("Tunnel / Baffle corner relay", Vector2.right, 0.8f, 1.25f, 0.76f, 0.7f, 0.24f, 0.45f, -0.51f, 0.35f, -0.78f, 0.08f, -0.84f, -0.25f);
    public static readonly WavePart[] BaffleSpinnerParts =
    {
        new WavePart("Baffle wall rebound", nameof(BaffleSpinner), BaffleSpinnerRoute0, WaveShape.Train, 3, 5.1f, 2, new[] { 0.3f, 0.49f, 0.71f }),
        new WavePart("Baffle reflected rank", nameof(BaffleSpinner), BaffleSpinnerRoute1, WaveShape.Rank, 2, 4.9f, 4, new[] { 0.34f, 0.62f }),
        new WavePart("Baffle corner relay", nameof(BaffleSpinner), BaffleSpinnerRoute2, WaveShape.Rake, 3, 5.3f, 7, new[] { 0.29f, 0.53f, 0.76f }),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedBaffleSpinner;
    public static Mesh BaffleSpinnerMesh()
    {
        if (cachedBaffleSpinner != null)
            return cachedBaffleSpinner;
        var b = new Builder();
        b.Drum(Vector3.zero, .38f, .3f, 6, 10, 11);
        foreach (int s in new[] { -1, 1 })
        {
            BevelArmor(b, new[] { V(s * .25f, -.1f), V(s * .6f, -.51f), V(s * .97f, -.2f), V(s * .8f, .52f), V(s * .4f, .73f), V(s * .62f, .25f) }, .18f, 0);
            b.Box(new Vector3(s * .64f, -.2f, -.16f), new Vector3(.19f, .2f, .17f), 11, 6);
            Motor(b, s * .3f, .55f, .18f, .27f, 0);
        }
        b.Drum(new Vector3(0, 0, -.22f), .19f, .12f, 10, 8);
        return cachedBaffleSpinner = b.Finish("BaffleSpinner authored hull and weapon lips");
    }
}
