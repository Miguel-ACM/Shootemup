using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class ReedSkimmer : DistrictEnemy
{
    protected override Vector2 CueAxis => Vector2.down;
    protected override void Configure()
    {
        hull = 61;
        hullScale = 0.49f;
        health = 20;
        attackInterval = Timing(2.7f, 1.9f);
        nextAttack = 1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        for (int side = -1; side <= 1; side += 2)
            Fire<SimpleRound>(Muzzle(side < 0 ? 0 : 1), CombatVisuals.Rotate(Vector2.down, side * (volley % 2 == 0 ? 18 : 8)), 2.6f, .26f);
    }

}

public static partial class WavePartLibrary
{
    static readonly FlightCurve ReedSkimmerRoute0 = Route("Fields / ReedSkimmer 0", Vector2.right, -.7f, 1.25f, -.62f, .88f, -.37f, .65f, .25f, .58f, .68f, .76f, .8f, 1.25f);
    static readonly FlightCurve ReedSkimmerRoute1 = Route("Fields / ReedSkimmer 1", Vector2.right, -1.4f, .72f, -.7f, .64f, 0, .55f, .72f, .61f, 1.4f, .8f);
    public static readonly WavePart[] ReedSkimmerParts ={
        new WavePart("ReedSkimmer call",nameof(ReedSkimmer),ReedSkimmerRoute0,WaveShape.Rake,3,4.65f,2, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
        new WavePart("ReedSkimmer crossing",nameof(ReedSkimmer),ReedSkimmerRoute1,WaveShape.Rake,3,4.90f,4, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
        new WavePart("ReedSkimmer answer",nameof(ReedSkimmer),ReedSkimmerRoute0,WaveShape.Train,3,5.15f,7, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedReedSkimmer;
    public static Mesh ReedSkimmerMesh()
    {
        if (cachedReedSkimmer != null)
            return cachedReedSkimmer;
        var b = new Builder();
        BevelArmor(b, new[] { V(0, -.92f), V(-.24f, -.25f), V(-.22f, .75f), V(.22f, .75f), V(.24f, -.25f) }, .28f, 0);
        foreach (int s in new[] { -1, 1 })
        {
            Armor(b, new[] { V(s * .16f, -.25f), V(s * 1.2f, .24f), V(s * .96f, .53f), V(s * .32f, .24f) }, .12f, 2);
            b.Box(new Vector3(s * .58f, -.3f, -.12f), new Vector3(.17f, .65f, .16f), 11, 6);
            b.Prism(new[] { V(s * .2f, .45f), V(s * .5f, .78f), V(s * .42f, .99f) }, .12f, 1, Vector3.zero, 6);
        }
        Glass(b, 0, -.08f, .25f, .42f, -.29f);
        Motor(b, 0, .64f, .28f, .32f, 0);
        return cachedReedSkimmer = b.Finish("ReedSkimmer authored hull and weapon lips");
    }
}
