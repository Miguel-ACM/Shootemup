using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class MirageObelisk : DistrictEnemy
{
    protected override Vector2 CueAxis => Vector2.down;
    protected override void Configure()
    {
        hull = 72;
        hullScale = 0.56f;
        health = 48;
        attackInterval = Timing(2.7f, 1.9f);
        nextAttack = 1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        int count = Shots(3, 5);
        for (int i = 0; i < count; i++)
            Fire<SuspendedRound>(Muzzle(i), CombatVisuals.Rotate(Vector2.down, (i - (count - 1) * .5f) * 24 + (volley % 2 == 0 ? 10 : -10)), 2.5f, .24f);
    }

}

public static partial class WavePartLibrary
{
    static readonly FlightCurve MirageObeliskRoute0 = Route("Desert / MirageObelisk 0", Vector2.right, .55f, 1.25f, .64f, .96f, .34f, .75f, -.21f, .65f, -.62f, .9f, -.63f, 1.25f);
    static readonly FlightCurve MirageObeliskRoute1 = Route("Desert / MirageObelisk 1", Vector2.right, -.55f, 1.25f, -.5f, .88f, -.15f, .67f, .4f, .71f, .65f, .96f, .7f, 1.25f);
    public static readonly WavePart[] MirageObeliskParts ={
        new WavePart("MirageObelisk call",nameof(MirageObelisk),MirageObeliskRoute0,WaveShape.Train,2,5.75f,2, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 2),
        new WavePart("MirageObelisk crossing",nameof(MirageObelisk),MirageObeliskRoute1,WaveShape.Train,2,6.00f,4, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 2),
        new WavePart("MirageObelisk answer",nameof(MirageObelisk),MirageObeliskRoute0,WaveShape.Rake,2,6.25f,7, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 2),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedMirageObelisk;
    public static Mesh MirageObeliskMesh()
    {
        if (cachedMirageObelisk != null)
            return cachedMirageObelisk;
        var b = new Builder();
        foreach (int s in new[] { -1, 1 })
        {
            BevelArmor(b, new[] { V(s * .23f, -.8f), V(s * .62f, -.8f), V(s * .7f, .71f), V(s * .43f, 1.08f), V(s * .18f, .71f) }, .37f, 13);
            Barrel(b, s * .43f, -.63f, .48f, .19f, -.2f);
            b.Box(new Vector3(s * .43f, .06f, -.27f), new Vector3(.09f, .8f, .08f), 10, 11);
            Motor(b, s * .43f, .79f, .2f, .3f, 13);
        }
        b.Box(new Vector3(0, .44f, .07f), new Vector3(.83f, .14f, .18f), 6, 11);
        b.Drum(new Vector3(0, .44f, -.1f), .19f, .15f, 11, 8);
        return cachedMirageObelisk = b.Finish("MirageObelisk authored hull and weapon lips");
    }
}
