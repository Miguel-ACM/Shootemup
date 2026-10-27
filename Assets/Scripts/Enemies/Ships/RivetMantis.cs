using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class RivetMantis : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 68;
        hullScale = 0.55f;
        health = 40;
        attackInterval = 2.3f;
        nextAttack = 1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        Fan<RicochetBolt>(5, 25, 3.1f, .21f);
    }

}

public static partial class WavePartLibrary
{
    static readonly FlightCurve RivetMantisRoute0 = Route("Tunnel / Mantis gantry traverse", Vector2.right, -1.4f, 0.92f, -0.63f, 0.89f, 0.1f, 0.87f, 0.65f, 0.63f, 1.4f, 0.58f);
    static readonly FlightCurve RivetMantisRoute1 = Route("Tunnel / Mantis lower bracket", Vector2.right, 0.6f, 1.25f, 0.58f, 0.7f, 0.4f, 0.47f, -0.16f, 0.38f, -0.7f, 0.39f, -1.4f, 0.4f);
    static readonly FlightCurve RivetMantisRoute2 = Route("Tunnel / Mantis return rail", Vector2.right, -1.4f, 0.52f, -0.66f, 0.49f, -0.05f, 0.55f, 0.38f, 0.83f, 0.43f, 1.25f);
    public static readonly WavePart[] RivetMantisParts =
    {
        new WavePart("Mantis gantry traverse", nameof(RivetMantis), RivetMantisRoute0, WaveShape.Rank, 2, 5.0f, 2, new[] { 0.28f, 0.61f }),
        new WavePart("Mantis lower bracket", nameof(RivetMantis), RivetMantisRoute1, WaveShape.Rake, 2, 5.4f, 4, new[] { 0.32f, 0.54f, 0.73f }),
        new WavePart("Mantis return rail", nameof(RivetMantis), RivetMantisRoute2, WaveShape.Train, 2, 5.2f, 7, new[] { 0.31f, 0.65f }),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedRivetMantis;
    public static Mesh RivetMantisMesh()
    {
        if (cachedRivetMantis != null)
            return cachedRivetMantis;
        var b = new Builder();
        BevelArmor(b, new[] { V(0, -.8f), V(-.27f, -.3f), V(-.2f, .74f), V(.2f, .74f), V(.27f, -.3f) }, .32f, 6);
        foreach (int s in new[] { -1, 1 })
        {
            Armor(b, new[] { V(s * .17f, .3f), V(s * .98f, .58f), V(s * 1.1f, -.24f), V(s * .78f, -.83f), V(s * .65f, -.75f), V(s * .76f, -.08f), V(s * .39f, .08f) }, .18f, 0);
            Barrel(b, s * .78f, -.55f, .44f, .15f, -.16f);
            Motor(b, s * .3f, .69f, .19f, .3f, 6);
        }
        Glass(b, 0, -.09f, .25f, .35f, -.31f);
        return cachedRivetMantis = b.Finish("RivetMantis authored hull and weapon lips");
    }
}
