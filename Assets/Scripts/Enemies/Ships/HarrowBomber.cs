using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class HarrowBomber : DistrictEnemy
{
    protected override Vector2 CueAxis => Vector2.down;
    protected override void Configure()
    {
        hull = 62;
        hullScale = 0.61f;
        health = 48;
        attackInterval = (2.6f);
        nextAttack = 1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        int count = (5);
        for (int i = 0; i < count; i++)
            Fire<SimpleRound>(Muzzle(i), CombatVisuals.Rotate(Vector2.down, (i - (count - 1) * .5f) * 24 + (volley % 2 == 0 ? 8 : -8)), 2.1f + Mathf.Abs(i - (count - 1) * .5f) * .18f, .29f);
    }

}

public static partial class WavePartLibrary
{
    static readonly FlightCurve HarrowBomberRoute0 = Route("Fields / Harrow parallel furrows", Vector2.right, -0.42f, 1.25f, -0.42f, 0.82f, -0.36f, 0.56f, -0.12f, 0.34f, 0.42f, 0.4f, 1.4f, 0.54f);
    static readonly FlightCurve HarrowBomberRoute1 = Route("Fields / Harrow crescent return", Vector2.right, -1.4f, 0.88f, -0.7f, 0.68f, 0.0f, 0.5f, 0.62f, 0.66f, 0.76f, 1.25f);
    static readonly FlightCurve HarrowBomberRoute2 = Route("Fields / Harrow staggered gate", Vector2.right, 0.62f, 1.25f, 0.55f, 0.82f, 0.3f, 0.61f, -0.34f, 0.5f, -0.73f, 0.66f, -1.4f, 0.8f);
    public static readonly WavePart[] HarrowBomberParts =
    {
        new WavePart("Harrow parallel furrows", nameof(HarrowBomber), HarrowBomberRoute0, WaveShape.Rank, 2, 5.7f, 2, new[] { 0.33f, 0.68f }),
        new WavePart("Harrow crescent return", nameof(HarrowBomber), HarrowBomberRoute1, WaveShape.Train, 2, 5.4f, 4, new[] { 0.3f, 0.62f }),
        new WavePart("Harrow staggered gate", nameof(HarrowBomber), HarrowBomberRoute2, WaveShape.Rake, 2, 5.3f, 7, new[] { 0.31f, 0.59f, 0.76f }),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedHarrowBomber;
    public static Mesh HarrowBomberMesh()
    {
        if (cachedHarrowBomber != null)
            return cachedHarrowBomber;
        var b = new Builder();
        foreach (int s in new[] { -1, 1 })
        {
            BevelArmor(b, new[] { V(s * .24f, -.48f), V(s * .53f, -.75f), V(s * .92f, -.55f), V(s * 1.06f, .46f), V(s * .73f, .88f), V(s * .34f, .54f) }, .32f, 2);
            b.Box(new Vector3(s * .72f, -.31f, -.16f), new Vector3(.28f, .54f, .23f), 11, 6);
            Motor(b, s * .7f, .64f, .25f, .4f, 0);
        }
        b.Box(new Vector3(0, .22f, 0), new Vector3(1.12f, .25f, .25f), 6, 11);
        Glass(b, 0, .21f, .4f, .25f, -.22f);
        return cachedHarrowBomber = b.Finish("HarrowBomber authored hull and weapon lips");
    }
}
