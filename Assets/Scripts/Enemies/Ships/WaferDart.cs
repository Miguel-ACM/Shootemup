using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class WaferDart : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 36;
        hullScale = .39f;
        health = 23;
        attackInterval = Timing(2.4f, 1.8f);
        Mover<Slalom>();
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        if (Difficulty.Easy)
            Cue(Muzzle(volley % 2), seconds);
        else
            firingCue.Warn(seconds);
    }

    protected override void Attack()
    {
        FanFrom<NeedleRound>(Shots(1, 2), 18, 5.3f, .18f, new Vector3(-.27f, -.78f, -.16f), new Vector3(.27f, -.78f, -.16f));
    }
}
public static partial class IndustrialMeshFactory
{
    static void BuildWaferDart(Builder b)
    {
        b.Prism(new[] { V(0, -1.05f), V(-.36f, -.24f), V(-.28f, .76f), V(.28f, .76f), V(.36f, -.24f) }, .26f, 13, Vector3.zero, 5);
        foreach (int s in new[] { -1, 1 })
        {
            b.Prism(new[] { V(s * .21f, -.3f), V(s * .68f, .41f), V(s * .55f, .85f), V(s * .20f, .50f) }, .14f, 4, new Vector3(0, 0, -.03f), 5);
            b.Box(new Vector3(s * .27f, -.54f, -.17f), new Vector3(.12f, .50f, .14f), 11, 4);
            b.Box(new Vector3(s * .24f, .73f, 0), new Vector3(.19f, .20f, .22f), 11, 4);
        }
        for (int i = 0; i < 3; i++)
            b.Box(new Vector3(0, .45f - i * .27f, -.18f), new Vector3(.42f, .20f, .13f), 4, 5);
        b.Prism(new[] { V(0, -.60f), V(-.13f, -.21f), V(.13f, -.21f) }, .08f, 9, new Vector3(0, 0, -.29f), 11);
    }
}
