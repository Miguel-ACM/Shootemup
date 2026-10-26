using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class BoneHarrier : DistrictEnemy
{
    protected override void Configure()
    {
        hull=80; hullScale=0.49f; health=32;
        attackInterval=Timing(2.7f,1.9f); nextAttack=1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        Fan<NeedleRound>(Shots(3,5),17,4.3f,.2f);
    }
    
}

public static partial class WavePartLibrary
{
    static readonly FlightCurve BoneHarrierRoute0=Route("Hell / BoneHarrier 0", Vector2.right, .65f,1.25f,.52f,.92f,.12f,.58f,-.45f,.42f,-.65f,.09f,-.2f,-.25f);
    static readonly FlightCurve BoneHarrierRoute1=Route("Hell / BoneHarrier 1", Vector2.right, -1.4f,.87f,-.74f,.76f,-.1f,.62f,.6f,.81f,1.4f,.89f);
    public static readonly WavePart[] BoneHarrierParts={
        new WavePart("BoneHarrier call",nameof(BoneHarrier),BoneHarrierRoute0,WaveShape.Train,3,4.35f,2, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
        new WavePart("BoneHarrier crossing",nameof(BoneHarrier),BoneHarrierRoute1,WaveShape.Train,3,4.60f,4, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
        new WavePart("BoneHarrier answer",nameof(BoneHarrier),BoneHarrierRoute0,WaveShape.Rake,3,4.85f,7, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedBoneHarrier;
    public static Mesh BoneHarrierMesh()
    {
        if(cachedBoneHarrier!=null) return cachedBoneHarrier;
        var b=new Builder();
        BevelArmor(b,new[]{V(0,-1.03f),V(-.23f,-.42f),V(-.29f,.34f),V(0,.74f),V(.29f,.34f),V(.23f,-.42f)},.36f,15);
        foreach(int s in new[]{-1,1}) {
            b.Prism(new[]{V(s*.2f,.08f),V(s*.62f,.75f),V(s*1.15f,.51f),V(s*.81f,.31f),V(s*.46f,-.35f)},.14f,6,Vector3.zero,13);
            b.Prism(new[]{V(s*.32f,.25f),V(s*.62f,.97f),V(s*.53f,.48f)},.14f,15,Vector3.zero,6);
            b.Box(new Vector3(s*.14f,-.03f,-.32f),new Vector3(.11f,.22f,.06f),10,11);
        }
        Barrel(b,0,-.75f,.42f,.15f,-.23f);
        return cachedBoneHarrier=b.Finish("BoneHarrier authored hull and weapon lips");
    }
}
