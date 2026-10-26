using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class RibbonGlider : DistrictEnemy
{
    protected override void Configure()
    {
        hull=73; hullScale=0.46f; health=23;
        attackInterval=Timing(2.7f,1.9f); nextAttack=1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        for(int s=-1;s<=1;s+=2) Fire<HelixRound>(Muzzle(s<0?0:1),CombatVisuals.Rotate(Vector2.down,s*26),2.8f,.2f);
    }
    
}

public static partial class WavePartLibrary
{
    static readonly FlightCurve RibbonGliderRoute0=Route("Candyland / RibbonGlider 0", Vector2.right, -.7f,1.25f,-.6f,.9f,-.1f,.53f,.5f,.72f,.7f,.95f,.62f,1.25f);
    static readonly FlightCurve RibbonGliderRoute1=Route("Candyland / RibbonGlider 1", Vector2.right, -1.4f,.83f,-.75f,.62f,-.1f,.78f,.6f,.58f,1.4f,.77f);
    public static readonly WavePart[] RibbonGliderParts={
        new WavePart("RibbonGlider call",nameof(RibbonGlider),RibbonGliderRoute0,WaveShape.Train,3,3.55f,2, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
        new WavePart("RibbonGlider crossing",nameof(RibbonGlider),RibbonGliderRoute1,WaveShape.Train,3,3.80f,4, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
        new WavePart("RibbonGlider answer",nameof(RibbonGlider),RibbonGliderRoute0,WaveShape.Rake,3,4.05f,7, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedRibbonGlider;
    public static Mesh RibbonGliderMesh()
    {
        if(cachedRibbonGlider!=null) return cachedRibbonGlider;
        var b=new Builder();
        BevelArmor(b,new[]{V(0,-.83f),V(-.2f,-.38f),V(-.16f,.73f),V(.16f,.73f),V(.2f,-.38f)},.26f,4);
        foreach(int s in new[]{-1,1}) {
            b.Prism(new[]{V(s*.12f,.06f),V(s*.9f,.65f),V(s*1.05f,.23f),V(s*.49f,-.44f)},.15f,8,Vector3.zero,14);
            b.Prism(new[]{V(s*.23f,.12f),V(s*.73f,.45f),V(s*.6f,.13f)},.08f,11,Vector3.back*.16f,3);
            Barrel(b,s*.49f,-.26f,.36f,.12f,-.16f); Motor(b,s*.22f,.65f,.16f,.24f,4);
        }
        b.Drum(new Vector3(0,.1f,-.24f),.18f,.15f,10,8);
        return cachedRibbonGlider=b.Finish("RibbonGlider authored hull and weapon lips");
    }
}
