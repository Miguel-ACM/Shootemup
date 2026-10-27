using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class BonbonSkirmisher : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 35;
        hullScale = .38f;
        health = 19;
        attackInterval = 1.7f;
        Mover<Slalom>();
    }

    protected override void Attack()
    {
        Fire<SimpleRound>(Muzzle(), Aim, 4.8f, .2f);
    }
}
public static partial class IndustrialMeshFactory
{
    static void BuildBonbon(Builder b)
    {
        b.Ball(Vector3.zero, new Vector3(.57f, .55f, .30f), 0);
        foreach (int s in new[] { -1, 1 })
        {
            b.Prism(new[] { V(s * .46f, -.15f), V(s * 1.02f, -.42f), V(s * .91f, .04f), V(s * 1.04f, .39f), V(s * .47f, .18f) }, .16f, 8, Vector3.zero, 14);
            b.Box(new Vector3(s * .45f, 0, -.18f), new Vector3(.13f, .41f, .18f), 10, 4);
            b.Box(new Vector3(s * .3f, .46f, .02f), new Vector3(.18f, .17f, .20f), 11, 6);
        }
        b.Prism(new[] { V(-.33f, -.07f), V(-.23f, .19f), V(.23f, .19f), V(.33f, -.07f), V(0, -.23f) }, .10f, 11, new Vector3(0, 0, -.3f), 14);
        b.Box(new Vector3(0, .03f, -.38f), new Vector3(.35f, .07f, .03f), 9, 11);
        b.Box(new Vector3(0, -.5f, -.1f), new Vector3(.17f, .26f, .16f), 4, 11);
    }
}
