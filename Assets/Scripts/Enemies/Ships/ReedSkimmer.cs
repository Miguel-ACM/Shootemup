using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class ReedSkimmer : DistrictEnemy
{
    protected override Vector2 CueAxis => Vector2.down;
    protected override void Configure()
    {
        hull = 61;
        hullScale = 0.49f;
        health = 20;
        attackInterval = (2.0f);
        nextAttack = 1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        for (int side = -1; side <= 1; side += 2)
            Fire<SimpleRound>(Muzzle(side < 0 ? 0 : 1), CombatVisuals.Rotate(Vector2.down, side * (volley % 2 == 0 ? 18 : 8)), 2.6f, .26f);
    }

}

public static partial class WavePartLibrary
{
    static readonly FlightCurve ReedSkimmerRoute0 = Route("Fields / Reedbank skimming", Vector2.right, -0.82f, 1.25f, -0.8f, 0.82f, -0.5f, 0.42f, 0.18f, 0.36f, 0.7f, 0.56f, 1.4f, 0.72f);
    static readonly FlightCurve ReedSkimmerRoute1 = Route("Fields / Riverbank opening", Vector2.right, -2.1f, 0.73f, -0.72f, 0.59f, -0.12f, 0.66f, 0.35f, 0.91f, 0.46f, 1.4f);
    static readonly FlightCurve ReedSkimmerRoute2 = Route("Fields / Reedbed switch", Vector2.right, -0.4f, 1.25f, -0.65f, 0.82f, -0.2f, 0.55f, 0.45f, 0.38f, 0.72f, 0.15f, 0.8f, -0.25f);
    public static readonly WavePart[] ReedSkimmerParts =
    {
        new WavePart("Reedbank skimming", nameof(ReedSkimmer), ReedSkimmerRoute0, WaveShape.Train, 4, 4.8f, 2, new[] { 0.28f, 0.51f, 0.74f }),
        new WavePart("Riverbank opening", nameof(ReedSkimmer), ReedSkimmerRoute1, WaveShape.Chevron, 3, 4.5f, 4, new[] { 0.32f, 0.62f }),
        new WavePart("Reedbed switch", nameof(ReedSkimmer), ReedSkimmerRoute2, WaveShape.Rake, 3, 5.0f, 7, new[] { 0.26f, 0.45f, 0.7f }),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedReedSkimmer;
    public static Mesh ReedSkimmerMesh()
    {
        if (cachedReedSkimmer != null)
            return cachedReedSkimmer;
        var b = new Builder();
        BevelArmor(b, new[] { V(0, -.92f), V(-.24f, -.25f), V(-.22f, .75f), V(.22f, .75f), V(.24f, -.25f) }, .28f, 0);
        foreach (int s in new[] { -1, 1 })
        {
            Armor(b, new[] { V(s * .16f, -.25f), V(s * 1.2f, .24f), V(s * .96f, .53f), V(s * .32f, .24f) }, .12f, 2);
            b.Box(new Vector3(s * .58f, -.3f, -.12f), new Vector3(.17f, .65f, .16f), 11, 6);
            b.Prism(new[] { V(s * .2f, .45f), V(s * .5f, .78f), V(s * .42f, .99f) }, .12f, 1, Vector3.zero, 6);
        }
        Glass(b, 0, -.08f, .25f, .42f, -.29f);
        Motor(b, 0, .64f, .28f, .32f, 0);
        return cachedReedSkimmer = b.Finish("ReedSkimmer authored hull and weapon lips");
    }
}
