using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class HailLantern : DistrictEnemy
{
    protected override Vector2 CueAxis => Vector2.down;
    protected override void Configure()
    {
        hull = 64;
        hullScale = 0.58f;
        health = 49;
        attackInterval = Timing(3.7f, 2.9f);
        nextAttack = 1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        var shell = Fire<BurstShell>(Muzzle(), Vector2.down, 2.4f, .48f);
        if (shell != null)
            shell.SetTravelDistance(volley % 2 == 0 ? 4.1f : 5.5f);
        if (!Difficulty.Easy)
            for (int s = -1; s <= 1; s += 2)
                Fire<SimpleRound>(Muzzle(), CombatVisuals.Rotate(Vector2.down, s * 38), 2.2f, .23f);
    }

}

public static partial class WavePartLibrary
{
    static readonly FlightCurve HailLanternRoute0 = Route("Snow / Lantern hanging crown", Vector2.right, -1.4f, 0.8f, -0.65f, 0.69f, 0.0f, 0.66f, 0.65f, 0.69f, 1.4f, 0.8f);
    static readonly FlightCurve HailLanternRoute1 = Route("Snow / Lantern falling bell", Vector2.right, -0.65f, 1.25f, -0.56f, 0.8f, -0.27f, 0.49f, 0.14f, 0.22f, 0.62f, 0.3f, 1.4f, 0.49f);
    static readonly FlightCurve HailLanternRoute2 = Route("Snow / Lantern winter arch", Vector2.right, 0.65f, 1.25f, 0.51f, 0.87f, 0.0f, 0.74f, -0.48f, 0.84f, -0.7f, 1.25f);
    public static readonly WavePart[] HailLanternParts =
    {
        new WavePart("Lantern hanging crown", nameof(HailLantern), HailLanternRoute0, WaveShape.Rank, 2, 5.8f, 2, new[] { 0.3f, 0.64f }, new[] { 0.49f }, 2),
        new WavePart("Lantern falling bell", nameof(HailLantern), HailLanternRoute1, WaveShape.Train, 2, 5.8f, 4, new[] { 0.28f, 0.57f }, new[] { 0.41f }, 2),
        new WavePart("Lantern winter arch", nameof(HailLantern), HailLanternRoute2, WaveShape.Rake, 2, 5.5f, 7, new[] { 0.34f, 0.68f }, new[] { 0.51f }, 2),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedHailLantern;
    public static Mesh HailLanternMesh()
    {
        if (cachedHailLantern != null)
            return cachedHailLantern;
        var b = new Builder();
        b.Drum(new Vector3(0, .1f, 0), .52f, .4f, 15, 8, 6);
        b.Drum(new Vector3(0, .1f, -.26f), .36f, .2f, 7, 8, 11);
        foreach (int s in new[] { -1, 1 })
        {
            b.Prism(new[] { V(s * .22f, .63f), V(s * .68f, 1), V(s * .55f, .1f) }, .16f, 15, Vector3.zero, 6);
            b.Box(new Vector3(s * .51f, -.18f, -.1f), new Vector3(.12f, .74f, .18f), 6, 11);
            Motor(b, s * .33f, .66f, .17f, .27f, 7);
        }
        b.Box(new Vector3(0, -.46f, -.19f), new Vector3(.3f, .6f, .25f), 11, 6);
        b.Drum(new Vector3(0, -.7f, -.32f), .17f, .1f, 10, 8, 11);
        return cachedHailLantern = b.Finish("HailLantern authored hull and weapon lips");
    }
}
