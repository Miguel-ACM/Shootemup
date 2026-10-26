using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class RibbonGlider : DistrictEnemy
{
    protected override Vector2 CueAxis => Vector2.down;
    protected override void Configure()
    {
        hull = 73;
        hullScale = 0.46f;
        health = 23;
        attackInterval = Timing(2.25f, 1.6f);
        nextAttack = 1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        for (int s = -1; s <= 1; s += 2)
            Fire<HelixRound>(Muzzle(s < 0 ? 0 : 1), CombatVisuals.Rotate(Vector2.down, s * 26), 2.8f, .2f);
    }

}

public static partial class WavePartLibrary
{
    static readonly FlightCurve RibbonGliderRoute0 = Route("Candyland / Ribbon unwinding bow", Vector2.right, -1.4f, 0.84f, -0.64f, 0.64f, 0.13f, 0.74f, 0.51f, 0.56f, 0.08f, 0.31f, -0.62f, 0.2f, -1.4f, 0.24f);
    static readonly FlightCurve RibbonGliderRoute1 = Route("Candyland / Ribbon falling streamer", Vector2.right, -0.7f, 1.25f, -0.35f, 0.9f, 0.34f, 0.66f, 0.52f, 0.35f, 0.05f, 0.06f, -0.46f, -0.25f);
    static readonly FlightCurve RibbonGliderRoute2 = Route("Candyland / Ribbon rising loop", Vector2.right, 1.4f, 0.33f, 0.66f, 0.3f, 0.07f, 0.48f, -0.46f, 0.78f, -0.56f, 1.25f);
    public static readonly WavePart[] RibbonGliderParts =
    {
        new WavePart("Ribbon unwinding bow", nameof(RibbonGlider), RibbonGliderRoute0, WaveShape.Train, 5, 3.5f, 2, new[] { 0.27f, 0.48f, 0.7f }, new[] { 0.39f, 0.72f }, 4),
        new WavePart("Ribbon falling streamer", nameof(RibbonGlider), RibbonGliderRoute1, WaveShape.Rake, 3, 3.25f, 4, new[] { 0.29f, 0.6f }, new[] { 0.44f }, 3),
        new WavePart("Ribbon rising loop", nameof(RibbonGlider), RibbonGliderRoute2, WaveShape.Chevron, 3, 3.4f, 7, new[] { 0.32f, 0.65f }, new[] { 0.48f }, 3),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedRibbonGlider;
    public static Mesh RibbonGliderMesh()
    {
        if (cachedRibbonGlider != null)
            return cachedRibbonGlider;
        var b = new Builder();
        BevelArmor(b, new[] { V(0, -.83f), V(-.2f, -.38f), V(-.16f, .73f), V(.16f, .73f), V(.2f, -.38f) }, .26f, 4);
        foreach (int s in new[] { -1, 1 })
        {
            b.Prism(new[] { V(s * .12f, .06f), V(s * .9f, .65f), V(s * 1.05f, .23f), V(s * .49f, -.44f) }, .15f, 8, Vector3.zero, 14);
            b.Prism(new[] { V(s * .23f, .12f), V(s * .73f, .45f), V(s * .6f, .13f) }, .08f, 11, Vector3.back * .16f, 3);
            Barrel(b, s * .49f, -.26f, .36f, .12f, -.16f);
            Motor(b, s * .22f, .65f, .16f, .24f, 4);
        }
        b.Drum(new Vector3(0, .1f, -.24f), .18f, .15f, 10, 8);
        return cachedRibbonGlider = b.Finish("RibbonGlider authored hull and weapon lips");
    }
}
