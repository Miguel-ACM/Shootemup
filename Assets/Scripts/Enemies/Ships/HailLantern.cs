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
        attackInterval = Timing(2.7f, 1.9f);
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
    static readonly FlightCurve HailLanternRoute0 = Route("Snow / HailLantern 0", Vector2.right, .66f, 1.25f, .62f, .94f, .29f, .73f, -.28f, .69f, -.63f, .88f, -.68f, 1.25f);
    static readonly FlightCurve HailLanternRoute1 = Route("Snow / HailLantern 1", Vector2.right, -.28f, 1.25f, -.25f, .96f, .05f, .75f, .38f, .63f, .55f, .82f, .45f, 1.25f);
    public static readonly WavePart[] HailLanternParts ={
        new WavePart("HailLantern call",nameof(HailLantern),HailLanternRoute0,WaveShape.Train,2,6.05f,2, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 2),
        new WavePart("HailLantern crossing",nameof(HailLantern),HailLanternRoute1,WaveShape.Train,2,6.30f,4, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 2),
        new WavePart("HailLantern answer",nameof(HailLantern),HailLanternRoute0,WaveShape.Rake,2,6.55f,7, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 2),
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
