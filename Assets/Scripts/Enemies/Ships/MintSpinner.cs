using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class MintSpinner : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 37;
        hullScale = .42f;
        health = 26;
        attackInterval = (2.0f);
        Mover<Slalom>();
    }

    protected override void Attack()
    {
        Fan<SimpleRound>((3), 31, 3.5f, .25f);
    }
}
public static partial class IndustrialMeshFactory
{
    static void BuildMintSpinner(Builder b)
    {
        b.Drum(Vector3.zero, .63f, .24f, 15, 12, 2);
        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.PI / 3;
            Vector2 p = new Vector2(Mathf.Cos(a), Mathf.Sin(a)), q = new Vector2(-p.y, p.x);
            b.Prism(new[] { p * .3f, p * .79f + q * .18f, p * .69f - q * .14f }, .12f, 2, new Vector3(0, 0, -.19f), 14);
        }
        b.Drum(new Vector3(0, 0, -.28f), .29f, .13f, 11, 8, 7);
        b.Drum(new Vector3(0, 0, -.37f), .16f, .08f, 9, 8, 2);
        foreach (int s in new[] { -1, 1 })
            b.Box(new Vector3(s * .34f, .44f, 0), new Vector3(.19f, .19f, .19f), 11, 2);
        b.Box(new Vector3(0, -.58f, -.08f), new Vector3(.18f, .30f, .16f), 11, 2);
    }
}
