using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class CapacitorKite : DistrictEnemy
{
    protected override Vector2 CueAxis => Vector2.down;
    protected override void Configure()
    {
        hull = 69;
        hullScale = 0.53f;
        health = 34;
        attackInterval = (2.15f);
        nextAttack = 1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        for (int s = -1; s <= 1; s += 2)
            Fire<PlasmaOrb>(Muzzle(s < 0 ? 0 : 1), CombatVisuals.Rotate(Vector2.down, s * 24), 2.0f, .34f);

        Fire<NeedleRound>(Muzzle(volley), Vector2.down, 4.1f, .17f);
    }

}

public static partial class WavePartLibrary
{
    static readonly FlightCurve CapacitorKiteRoute0 = Route("Thunder / Kite stormward curl", Vector2.right, -0.68f, 1.25f, -0.75f, 0.81f, -0.45f, 0.55f, 0.14f, 0.6f, 0.52f, 0.84f, 0.46f, 1.25f);
    static readonly FlightCurve CapacitorKiteRoute1 = Route("Thunder / Kite cloud shoulder", Vector2.right, -1.4f, 0.83f, -0.71f, 0.57f, -0.18f, 0.65f, 0.22f, 0.95f, 0.7f, 1.25f);
    static readonly FlightCurve CapacitorKiteRoute2 = Route("Thunder / Kite outer eddy", Vector2.right, 1.4f, 0.91f, 0.71f, 0.8f, 0.29f, 0.52f, -0.17f, 0.43f, -0.64f, 0.66f, -0.8f, 1.25f);
    public static readonly WavePart[] CapacitorKiteParts =
    {
        new WavePart("Kite stormward curl", nameof(CapacitorKite), CapacitorKiteRoute0, WaveShape.Train, 3, 5.2f, 2, new[] { 0.29f, 0.51f, 0.72f }),
        new WavePart("Kite cloud shoulder", nameof(CapacitorKite), CapacitorKiteRoute1, WaveShape.Chevron, 3, 5.0f, 4, new[] { 0.33f, 0.62f }),
        new WavePart("Kite outer eddy", nameof(CapacitorKite), CapacitorKiteRoute2, WaveShape.Rake, 3, 5.6f, 7, new[] { 0.27f, 0.47f, 0.69f }),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedCapacitorKite;
    public static Mesh CapacitorKiteMesh()
    {
        if (cachedCapacitorKite != null)
            return cachedCapacitorKite;
        var b = new Builder();
        foreach (int s in new[] { -1, 1 })
        {
            BevelArmor(b, new[] { V(s * .08f, -.75f), V(s * .95f, .02f), V(s * .5f, .9f), V(s * .2f, .29f) }, .2f, 7);
            b.Drum(new Vector3(s * .52f, -.18f, -.2f), .19f, .18f, 10, 8, 11);
            Barrel(b, s * .52f, -.3f, .36f, .18f, -.26f);
            Motor(b, s * .28f, .61f, .18f, .31f, 7);
        }
        b.Box(new Vector3(0, .04f, -.2f), new Vector3(.17f, .6f, .2f), 11, 6);
        return cachedCapacitorKite = b.Finish("CapacitorKite authored hull and weapon lips");
    }
}
