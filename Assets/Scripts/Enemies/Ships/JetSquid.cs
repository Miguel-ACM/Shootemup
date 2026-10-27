using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class JetSquid : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 78;
        hullScale = 0.51f;
        health = 31;
        attackInterval = 1.95f;
        nextAttack = 1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        Fan<NeedleRound>(3, 19, 3.6f, .21f);
    }

}

public static partial class WavePartLibrary
{
    static readonly FlightCurve JetSquidRoute0 = Route("Underwater / Squid inkward dive", Vector2.right, -0.52f, 1.25f, -0.34f, 0.83f, 0.32f, 0.57f, 0.57f, 0.29f, 0.29f, 0.09f, -0.46f, -0.25f);
    static readonly FlightCurve JetSquidRoute1 = Route("Underwater / Squid rising jet", Vector2.right, -1.4f, 0.28f, -0.67f, 0.4f, 0.01f, 0.65f, 0.54f, 0.94f, 0.65f, 1.25f);
    static readonly FlightCurve JetSquidRoute2 = Route("Underwater / Squid reef scissors", Vector2.right, 2.1f, 0.77f, 0.7f, 0.6f, 0.08f, 0.36f, -0.53f, 0.48f, -0.8f, 0.86f, -0.78f, 1.4f);
    public static readonly WavePart[] JetSquidParts =
    {
        new WavePart("Squid inkward dive", nameof(JetSquid), JetSquidRoute0, WaveShape.Train, 4, 4.3f, 2, new[] { 0.3f, 0.58f }),
        new WavePart("Squid rising jet", nameof(JetSquid), JetSquidRoute1, WaveShape.Rake, 3, 4.2f, 4, new[] { 0.31f, 0.65f }),
        new WavePart("Squid reef scissors", nameof(JetSquid), JetSquidRoute2, WaveShape.Chevron, 3, 4.6f, 7, new[] { 0.28f, 0.61f }),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedJetSquid;
    public static Mesh JetSquidMesh()
    {
        if (cachedJetSquid != null)
            return cachedJetSquid;
        var b = new Builder();
        BevelArmor(b, new[] { V(0, .95f), V(-.37f, .36f), V(-.32f, -.34f), V(0, -.64f), V(.32f, -.34f), V(.37f, .36f) }, .4f, 7);
        foreach (int s in new[] { -1, 1 })
        {
            Armor(b, new[] { V(s * .22f, .42f), V(s * .8f, .08f), V(s * .44f, -.2f) }, .09f, 15);
            for (int i = 0; i < 2; i++)
                b.Prism(new[] { V(s * (.12f + i * .2f), -.3f), V(s * (.3f + i * .21f), -.58f), V(s * (.15f + i * .25f), -1.1f), V(s * (.09f + i * .16f), -.68f) }, .09f, 13, Vector3.zero, 6);
        }
        Barrel(b, 0, -.68f, .48f, .16f, -.22f);
        Glass(b, 0, .01f, .36f, .26f, -.36f);
        return cachedJetSquid = b.Finish("JetSquid authored hull and weapon lips");
    }
}
