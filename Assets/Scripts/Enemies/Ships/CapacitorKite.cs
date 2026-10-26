using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class CapacitorKite : DistrictEnemy
{
    protected override Vector2 CueAxis => Vector2.down;
    protected override void Configure()
    {
        hull = 69;
        hullScale = 0.53f;
        health = 34;
        attackInterval = Timing(2.7f, 1.9f);
        nextAttack = 1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        for (int s = -1; s <= 1; s += 2)
            Fire<PlasmaOrb>(Muzzle(s < 0 ? 0 : 1), CombatVisuals.Rotate(Vector2.down, s * 24), 2.0f, .34f);
        if (!Difficulty.Easy)
            Fire<NeedleRound>(Muzzle(volley), Vector2.down, 4.1f, .17f);
    }

}

public static partial class WavePartLibrary
{
    static readonly FlightCurve CapacitorKiteRoute0 = Route("Thunder / CapacitorKite 0", Vector2.right, -.7f, 1.25f, -.55f, .93f, -.32f, .67f, .23f, .6f, .6f, .8f, .72f, 1.25f);
    static readonly FlightCurve CapacitorKiteRoute1 = Route("Thunder / CapacitorKite 1", Vector2.right, -1.4f, .86f, -.8f, .76f, -.1f, .7f, .7f, .79f, 1.4f, .85f);
    public static readonly WavePart[] CapacitorKiteParts ={
        new WavePart("CapacitorKite call",nameof(CapacitorKite),CapacitorKiteRoute0,WaveShape.Rake,3,5.05f,2, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
        new WavePart("CapacitorKite crossing",nameof(CapacitorKite),CapacitorKiteRoute1,WaveShape.Rake,3,5.30f,4, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
        new WavePart("CapacitorKite answer",nameof(CapacitorKite),CapacitorKiteRoute0,WaveShape.Train,3,5.55f,7, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedCapacitorKite;
    public static Mesh CapacitorKiteMesh()
    {
        if (cachedCapacitorKite != null)
            return cachedCapacitorKite;
        var b = new Builder();
        foreach (int s in new[] { -1, 1 })
        {
            BevelArmor(b, new[] { V(s * .08f, -.75f), V(s * .95f, .02f), V(s * .5f, .9f), V(s * .2f, .29f) }, .2f, 7);
            b.Drum(new Vector3(s * .52f, -.18f, -.2f), .19f, .18f, 10, 8, 11);
            Barrel(b, s * .52f, -.3f, .36f, .18f, -.26f);
            Motor(b, s * .28f, .61f, .18f, .31f, 7);
        }
        b.Box(new Vector3(0, .04f, -.2f), new Vector3(.17f, .6f, .2f), 11, 6);
        return cachedCapacitorKite = b.Finish("CapacitorKite authored hull and weapon lips");
    }
}
