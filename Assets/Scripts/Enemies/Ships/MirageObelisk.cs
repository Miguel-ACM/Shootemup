using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class MirageObelisk : DistrictEnemy
{
    protected override Vector2 CueAxis => Vector2.down;
    protected override void Configure()
    {
        hull = 72;
        hullScale = 0.56f;
        health = 48;
        attackInterval = (2.65f);
        nextAttack = 1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        int count = (5);
        for (int i = 0; i < count; i++)
            Fire<SuspendedRound>(Muzzle(i), CombatVisuals.Rotate(Vector2.down, (i - (count - 1) * .5f) * 24 + (volley % 2 == 0 ? 10 : -10)), 2.5f, .24f);
    }

}

public static partial class WavePartLibrary
{
    static readonly FlightCurve MirageObeliskRoute0 = Route("Desert / Obelisk mirage march", Vector2.right, -0.46f, 1.25f, -0.46f, 0.89f, -0.4f, 0.64f, -0.31f, 0.4f, -0.22f, 0.14f, -0.19f, -0.25f);
    static readonly FlightCurve MirageObeliskRoute1 = Route("Desert / Obelisk buried arc", Vector2.right, -1.4f, 0.88f, -0.68f, 0.66f, -0.08f, 0.58f, 0.5f, 0.65f, 0.74f, 0.92f, 0.72f, 1.25f);
    static readonly FlightCurve MirageObeliskRoute2 = Route("Desert / Obelisk paired descent", Vector2.right, 0.58f, 1.25f, 0.52f, 0.84f, 0.27f, 0.59f, -0.1f, 0.4f, -0.62f, 0.21f, -1.4f, 0.16f);
    public static readonly WavePart[] MirageObeliskParts =
    {
        new WavePart("Obelisk mirage march", nameof(MirageObelisk), MirageObeliskRoute0, WaveShape.Rank, 2, 5.6f, 2, new[] { 0.28f, 0.58f, 0.75f }),
        new WavePart("Obelisk buried arc", nameof(MirageObelisk), MirageObeliskRoute1, WaveShape.Train, 2, 5.8f, 4, new[] { 0.31f, 0.64f }),
        new WavePart("Obelisk paired descent", nameof(MirageObelisk), MirageObeliskRoute2, WaveShape.Rake, 2, 5.6f, 7, new[] { 0.3f, 0.53f, 0.74f }),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedMirageObelisk;
    public static Mesh MirageObeliskMesh()
    {
        if (cachedMirageObelisk != null)
            return cachedMirageObelisk;
        var b = new Builder();
        foreach (int s in new[] { -1, 1 })
        {
            BevelArmor(b, new[] { V(s * .23f, -.8f), V(s * .62f, -.8f), V(s * .7f, .71f), V(s * .43f, 1.08f), V(s * .18f, .71f) }, .37f, 13);
            Barrel(b, s * .43f, -.63f, .48f, .19f, -.2f);
            b.Box(new Vector3(s * .43f, .06f, -.27f), new Vector3(.09f, .8f, .08f), 10, 11);
            Motor(b, s * .43f, .79f, .2f, .3f, 13);
        }
        b.Box(new Vector3(0, .44f, .07f), new Vector3(.83f, .14f, .18f), 6, 11);
        b.Drum(new Vector3(0, .44f, -.1f), .19f, .15f, 11, 8);
        return cachedMirageObelisk = b.Finish("MirageObelisk authored hull and weapon lips");
    }
}
