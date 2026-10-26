using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class GumdropMortar : DistrictEnemy
{
    protected override Vector2 CueAxis => Vector2.down;
    protected override void Configure()
    {
        hull = 74;
        hullScale = 0.53f;
        health = 35;
        attackInterval = Timing(3.45f, 2.6f);
        nextAttack = 1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        var shell = Fire<BurstShell>(Muzzle(volley), CombatVisuals.Rotate(Vector2.down, volley % 2 == 0 ? -15 : 15), 2.4f, .46f);
        if (shell != null)
            shell.SetTravelDistance(4.3f);
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        Cue(Muzzle(volley + 1), seconds);
    }

}

public static partial class WavePartLibrary
{
    static readonly FlightCurve GumdropMortarRoute0 = Route("Candyland / Gumdrop sugar fall", Vector2.right, -0.62f, 1.25f, -0.52f, 0.85f, -0.28f, 0.54f, 0.26f, 0.35f, 0.73f, 0.46f, 1.4f, 0.59f);
    static readonly FlightCurve GumdropMortarRoute1 = Route("Candyland / Gumdrop candy shelf", Vector2.right, -1.4f, 0.87f, -0.66f, 0.72f, 0.04f, 0.73f, 0.7f, 0.84f, 1.4f, 0.9f);
    static readonly FlightCurve GumdropMortarRoute2 = Route("Candyland / Gumdrop wrapping turn", Vector2.right, 0.62f, 1.25f, 0.58f, 0.82f, 0.25f, 0.59f, -0.31f, 0.55f, -0.72f, 0.74f, -0.8f, 1.25f);
    public static readonly WavePart[] GumdropMortarParts =
    {
        new WavePart("Gumdrop sugar fall", nameof(GumdropMortar), GumdropMortarRoute0, WaveShape.Train, 3, 4.1f, 2, new[] { 0.3f, 0.65f }, new[] { 0.47f }, 2),
        new WavePart("Gumdrop candy shelf", nameof(GumdropMortar), GumdropMortarRoute1, WaveShape.Rank, 2, 4.2f, 4, new[] { 0.33f, 0.66f }, new[] { 0.5f }, 2),
        new WavePart("Gumdrop wrapping turn", nameof(GumdropMortar), GumdropMortarRoute2, WaveShape.Rake, 2, 4.3f, 7, new[] { 0.29f, 0.61f }, new[] { 0.46f }, 2),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedGumdropMortar;
    public static Mesh GumdropMortarMesh()
    {
        if (cachedGumdropMortar != null)
            return cachedGumdropMortar;
        var b = new Builder();
        b.Drum(Vector3.zero, .56f, .38f, 8, 10, 4);
        BevelArmor(b, new[] { V(-.42f, -.34f), V(.42f, -.34f), V(.49f, .25f), V(.22f, .67f), V(-.22f, .67f), V(-.49f, .25f) }, .29f, 10, -.22f);
        foreach (int s in new[] { -1, 1 })
        {
            b.Box(new Vector3(s * .46f, -.29f, -.21f), new Vector3(.21f, .58f, .19f), 11, 14);
            b.Drum(new Vector3(s * .57f, .18f, -.08f), .21f, .16f, 3, 8, 4);
            Motor(b, s * .29f, .55f, .18f, .24f, 4);
        }
        Glass(b, 0, -.1f, .38f, .2f, -.43f);
        return cachedGumdropMortar = b.Finish("GumdropMortar authored hull and weapon lips");
    }
}
