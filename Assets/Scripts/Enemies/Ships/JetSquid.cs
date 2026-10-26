using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class JetSquid : DistrictEnemy
{
    protected override void Configure()
    {
        hull=78; hullScale=0.51f; health=31;
        attackInterval=Timing(2.7f,1.9f); nextAttack=1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        Fan<NeedleRound>(Shots(2,3),19,3.6f,.21f);
    }
    
}

public static partial class WavePartLibrary
{
    static readonly FlightCurve JetSquidRoute0=Route("Underwater / JetSquid 0", Vector2.right, -.6f,1.25f,-.52f,.92f,-.12f,.59f,.48f,.44f,.6f,.1f,.2f,-.25f);
    static readonly FlightCurve JetSquidRoute1=Route("Underwater / JetSquid 1", Vector2.right, 1.4f,.87f,.72f,.8f,.15f,.59f,-.6f,.68f,-1.4f,.88f);
    public static readonly WavePart[] JetSquidParts={
        new WavePart("JetSquid call",nameof(JetSquid),JetSquidRoute0,WaveShape.Train,3,4.55f,2, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
        new WavePart("JetSquid crossing",nameof(JetSquid),JetSquidRoute1,WaveShape.Train,3,4.80f,4, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
        new WavePart("JetSquid answer",nameof(JetSquid),JetSquidRoute0,WaveShape.Rake,3,5.05f,7, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedJetSquid;
    public static Mesh JetSquidMesh()
    {
        if(cachedJetSquid!=null) return cachedJetSquid;
        var b=new Builder();
        BevelArmor(b,new[]{V(0,.95f),V(-.37f,.36f),V(-.32f,-.34f),V(0,-.64f),V(.32f,-.34f),V(.37f,.36f)},.4f,7);
        foreach(int s in new[]{-1,1}) {
            Armor(b,new[]{V(s*.22f,.42f),V(s*.8f,.08f),V(s*.44f,-.2f)},.09f,15);
            for(int i=0;i<2;i++) b.Prism(new[]{V(s*(.12f+i*.2f),-.3f),V(s*(.3f+i*.21f),-.58f),V(s*(.15f+i*.25f),-1.1f),V(s*(.09f+i*.16f),-.68f)},.09f,13,Vector3.zero,6);
        }
        Barrel(b,0,-.68f,.48f,.16f,-.22f);
        Glass(b,0,.01f,.36f,.26f,-.36f);
        return cachedJetSquid=b.Finish("JetSquid authored hull and weapon lips");
    }
}
