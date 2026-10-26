using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class IcicleWeaver : DistrictEnemy
{
    protected override Vector2 CueAxis => Vector2.down;
    protected override void Configure()
    {
        hull = 63;
        hullScale = 0.49f;
        health = 27;
        attackInterval = Timing(2.8f, 2.0f);
        nextAttack = 1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        int count = Shots(3, 5);
        for (int i = 0; i < count; i++)
            Fire<SuspendedRound>(Muzzle(i), CombatVisuals.Rotate(Vector2.down, (i - (count - 1) * .5f) * 19 + (volley % 2 == 0 ? 8 : -8)), 2.9f, .19f);
    }

}

public static partial class WavePartLibrary
{
    static readonly FlightCurve IcicleWeaverRoute0 = Route("Snow / Icicle diagonal loom", Vector2.right, -0.7f, 1.25f, -0.58f, 0.92f, -0.1f, 0.72f, 0.53f, 0.5f, 0.62f, 0.18f, 0.64f, -0.25f);
    static readonly FlightCurve IcicleWeaverRoute1 = Route("Snow / Icicle folding thread", Vector2.right, -1.4f, 0.93f, -0.67f, 0.79f, 0.24f, 0.72f, 0.54f, 0.49f, -0.12f, 0.29f, -1.4f, 0.19f);
    static readonly FlightCurve IcicleWeaverRoute2 = Route("Snow / Icicle closing seam", Vector2.right, 0.6f, 1.25f, 0.52f, 0.81f, 0.12f, 0.57f, -0.48f, 0.43f, -0.65f, 0.1f, -0.7f, -0.25f);
    public static readonly WavePart[] IcicleWeaverParts =
    {
        new WavePart("Icicle diagonal loom", nameof(IcicleWeaver), IcicleWeaverRoute0, WaveShape.Rake, 3, 5.2f, 2, new[] { 0.25f, 0.48f, 0.7f }, new[] { 0.36f, 0.7f }, 3),
        new WavePart("Icicle folding thread", nameof(IcicleWeaver), IcicleWeaverRoute1, WaveShape.Train, 4, 5.6f, 4, new[] { 0.27f, 0.39f, 0.66f }, new[] { 0.43f, 0.7f }, 3),
        new WavePart("Icicle closing seam", nameof(IcicleWeaver), IcicleWeaverRoute2, WaveShape.Rank, 3, 5.0f, 7, new[] { 0.32f, 0.55f, 0.75f }, new[] { 0.41f, 0.74f }, 3),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedIcicleWeaver;
    public static Mesh IcicleWeaverMesh()
    {
        if (cachedIcicleWeaver != null)
            return cachedIcicleWeaver;
        var b = new Builder();
        BevelArmor(b, new[] { V(-.2f, -.45f), V(0, -.77f), V(.2f, -.45f), V(.25f, .68f), V(-.25f, .68f) }, .3f, 7);
        foreach (int s in new[] { -1, 1 })
        {
            Armor(b, new[] { V(s * .15f, .12f), V(s * .85f, .73f), V(s * .62f, -.73f), V(s * .46f, -.78f), V(s * .38f, -.06f) }, .16f, 15);
            Barrel(b, s * .55f, -.49f, .57f, .13f, -.18f);
            Motor(b, s * .37f, .59f, .18f, .28f, 7);
        }
        Glass(b, 0, .12f, .27f, .4f, -.28f);
        return cachedIcicleWeaver = b.Finish("IcicleWeaver authored hull and weapon lips");
    }
}
