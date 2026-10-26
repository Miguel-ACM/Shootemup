using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class CometTug : DistrictEnemy
{
    protected override Vector2 CueAxis => Vector2.down;
    protected override void Configure()
    {
        hull = 76;
        hullScale = 0.55f;
        health = 47;
        attackInterval = Timing(3.25f, 2.25f);
        nextAttack = 1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        int n = Shots(3, 5);
        for (int i = 0; i < n; i++)
            Fire<SuspendedRound>(Muzzle(i), CombatVisuals.Rotate(Vector2.down, (i - (n - 1) * .5f) * 22 + (volley % 2 == 0 ? 8 : -8)), 3.2f, .23f);
    }

}

public static partial class WavePartLibrary
{
    static readonly FlightCurve CometTugRoute0 = Route("Space / Comet gravitational hook", Vector2.right, -0.63f, 1.25f, -0.72f, 0.85f, -0.6f, 0.46f, -0.08f, 0.29f, 0.58f, 0.47f, 1.4f, 0.76f);
    static readonly FlightCurve CometTugRoute1 = Route("Space / Comet apogee rake", Vector2.right, -1.4f, 0.32f, -0.73f, 0.47f, -0.18f, 0.77f, 0.44f, 0.93f, 0.71f, 1.25f);
    static readonly FlightCurve CometTugRoute2 = Route("Space / Comet trailing orbit", Vector2.right, 0.64f, 1.25f, 0.72f, 0.8f, 0.38f, 0.48f, -0.22f, 0.51f, -0.71f, 0.79f, -1.4f, 0.89f);
    public static readonly WavePart[] CometTugParts =
    {
        new WavePart("Comet gravitational hook", nameof(CometTug), CometTugRoute0, WaveShape.Train, 2, 5.9f, 2, new[] { 0.28f, 0.44f, 0.65f }, new[] { 0.42f, 0.73f }, 2),
        new WavePart("Comet apogee rake", nameof(CometTug), CometTugRoute1, WaveShape.Rake, 2, 5.7f, 4, new[] { 0.3f, 0.54f, 0.74f }, new[] { 0.43f, 0.75f }, 2),
        new WavePart("Comet trailing orbit", nameof(CometTug), CometTugRoute2, WaveShape.Rank, 2, 5.6f, 7, new[] { 0.27f, 0.46f, 0.68f }, new[] { 0.4f, 0.72f }, 2),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedCometTug;
    public static Mesh CometTugMesh()
    {
        if (cachedCometTug != null)
            return cachedCometTug;
        var b = new Builder();
        BevelArmor(b, new[] { V(-.28f, -.6f), V(.28f, -.6f), V(.38f, .5f), V(-.18f, .8f), V(-.38f, .4f) }, .38f, 15);
        b.Box(new Vector3(-.45f, -.26f, 0), new Vector3(.24f, 1.44f, .24f), 13, 6);
        b.Box(new Vector3(.68f, -.1f, 0), new Vector3(.23f, 1.22f, .23f), 13, 6);
        b.Box(new Vector3(.2f, .19f, .06f), new Vector3(1.9f, .1f, .14f), 15, 6);
        SolarPanel(b, new Vector3(-.95f, .2f, .08f), .49f, 1.2f);
        SpaceDish(b, new Vector3(.88f, .59f, -.1f), .28f);
        Barrel(b, -.45f, -.75f, .46f, .18f, -.2f);
        Barrel(b, .68f, -.49f, .44f, .18f, -.2f);
        b.Drum(new Vector3(0, .13f, -.3f), .23f, .17f, 11, 8, 13);
        return cachedCometTug = b.Finish("CometTug authored hull and weapon lips");
    }
}
