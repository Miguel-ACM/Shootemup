using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class StormOrgan : DistrictEnemy
{
    protected override Vector2 CueAxis => Vector2.down;
    protected override void Configure()
    {
        hull = 70;
        hullScale = 0.57f;
        health = 48;
        attackInterval = (3.0f);
        nextAttack = 1.4f;
        Mover<Slalom>();
    }

    protected override void Attack()
    {
        var local = EnemyWeaponLayout.Ports(hull)[volley % 2];
        var beam = Fire<PulseLaser>(Hull.TransformPoint(local), Vector2.down, 0);
        if (beam != null)
        {
            beam.Bind(Hull, local);
            beam.warning = (1.05f);
            beam.activeTime = (.95f);
            beam.width = .22f;
        }
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        Cue(Muzzle((volley + 1) % 2), seconds);
    }

}

public static partial class WavePartLibrary
{
    static readonly FlightCurve StormOrganRoute0 = Route("Thunder / Organ vertical aisle", Vector2.right, -0.48f, 1.25f, -0.49f, 0.94f, -0.48f, 0.71f, -0.43f, 0.48f, -0.12f, 0.27f, 0.57f, 0.3f, 1.4f, 0.4f);
    static readonly FlightCurve StormOrganRoute1 = Route("Thunder / Organ descending columns", Vector2.right, -0.55f, 1.25f, -0.5f, 0.89f, -0.42f, 0.65f, -0.32f, 0.43f, -0.22f, 0.16f, -0.15f, -0.25f);
    static readonly FlightCurve StormOrganRoute2 = Route("Thunder / Organ lightning terrace", Vector2.right, -1.4f, 0.9f, -0.68f, 0.79f, 0.0f, 0.78f, 0.69f, 0.79f, 1.4f, 0.9f);
    public static readonly WavePart[] StormOrganParts =
    {
        new WavePart("Organ vertical aisle", nameof(StormOrgan), StormOrganRoute0, WaveShape.Rank, 2, 6.4f, 2, new[] { 0.27f, 0.6f }),
        new WavePart("Organ descending columns", nameof(StormOrgan), StormOrganRoute1, WaveShape.Train, 2, 6.0f, 4, new[] { 0.3f, 0.65f }),
        new WavePart("Organ lightning terrace", nameof(StormOrgan), StormOrganRoute2, WaveShape.Rake, 2, 6.1f, 7, new[] { 0.29f, 0.62f }),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedStormOrgan;
    public static Mesh StormOrganMesh()
    {
        if (cachedStormOrgan != null)
            return cachedStormOrgan;
        var b = new Builder();
        b.Box(new Vector3(0, .15f, 0), new Vector3(.9f, .55f, .27f), 7, 6);
        foreach (int s in new[] { -1, 1 })
        {
            BevelArmor(b, new[] { V(s * .2f, -.82f), V(s * .55f, -.82f), V(s * .62f, .86f), V(s * .3f, 1.04f) }, .32f, 15);
            Barrel(b, s * .38f, -.6f, .84f, .2f, -.23f);
            b.Drum(new Vector3(s * .38f, .1f, -.3f), .18f, .16f, 10, 8, 11);
            Motor(b, s * .38f, .82f, .22f, .3f, 7);
        }
        b.Box(new Vector3(0, .52f, -.12f), new Vector3(1.48f, .09f, .13f), 6, 11);
        return cachedStormOrgan = b.Finish("StormOrgan authored hull and weapon lips");
    }
}
