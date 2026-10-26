using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class BoneHarrier : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 80;
        hullScale = 0.49f;
        health = 32;
        attackInterval = Timing(2.3f, 1.5f);
        nextAttack = 1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        Fan<NeedleRound>(Shots(3, 5), 17, 4.3f, .2f);
    }

}

public static partial class WavePartLibrary
{
    static readonly FlightCurve BoneHarrierRoute0 = Route("Hell / Harrier talon dive", Vector2.right, -0.62f, 1.25f, -0.5f, 0.8f, 0.1f, 0.47f, 0.6f, 0.22f, 1.4f, 0.14f);
    static readonly FlightCurve BoneHarrierRoute1 = Route("Hell / Harrier scything ascent", Vector2.right, -1.4f, 0.22f, -0.68f, 0.3f, 0.04f, 0.55f, 0.53f, 0.89f, 0.59f, 1.25f);
    static readonly FlightCurve BoneHarrierRoute2 = Route("Hell / Harrier blackwing hook", Vector2.right, 1.4f, 0.86f, 0.7f, 0.7f, 0.22f, 0.36f, -0.31f, 0.24f, -0.71f, 0.47f, -1.4f, 0.67f);
    public static readonly WavePart[] BoneHarrierParts =
    {
        new WavePart("Harrier talon dive", nameof(BoneHarrier), BoneHarrierRoute0, WaveShape.Train, 5, 3.7f, 2, new[] { 0.25f, 0.42f, 0.6f, 0.75f }, new[] { 0.36f, 0.61f, 0.77f }, 4),
        new WavePart("Harrier scything ascent", nameof(BoneHarrier), BoneHarrierRoute1, WaveShape.Chevron, 3, 3.8f, 4, new[] { 0.28f, 0.47f, 0.7f }, new[] { 0.4f, 0.72f }, 3),
        new WavePart("Harrier blackwing hook", nameof(BoneHarrier), BoneHarrierRoute2, WaveShape.Rake, 3, 4.1f, 7, new[] { 0.24f, 0.4f, 0.6f, 0.76f }, new[] { 0.35f, 0.58f, 0.76f }, 3),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedBoneHarrier;
    public static Mesh BoneHarrierMesh()
    {
        if (cachedBoneHarrier != null)
            return cachedBoneHarrier;
        var b = new Builder();
        BevelArmor(b, new[] { V(0, -1.03f), V(-.23f, -.42f), V(-.29f, .34f), V(0, .74f), V(.29f, .34f), V(.23f, -.42f) }, .36f, 15);
        foreach (int s in new[] { -1, 1 })
        {
            b.Prism(new[] { V(s * .2f, .08f), V(s * .62f, .75f), V(s * 1.15f, .51f), V(s * .81f, .31f), V(s * .46f, -.35f) }, .14f, 6, Vector3.zero, 13);
            b.Prism(new[] { V(s * .32f, .25f), V(s * .62f, .97f), V(s * .53f, .48f) }, .14f, 15, Vector3.zero, 6);
            b.Box(new Vector3(s * .14f, -.03f, -.32f), new Vector3(.11f, .22f, .06f), 10, 11);
        }
        Barrel(b, 0, -.75f, .42f, .15f, -.23f);
        return cachedBoneHarrier = b.Finish("BoneHarrier authored hull and weapon lips");
    }
}
