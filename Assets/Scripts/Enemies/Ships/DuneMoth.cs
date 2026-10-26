using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class DuneMoth : DistrictEnemy
{
    protected override void Configure()
    {
        hull=71; hullScale=0.53f; health=32;
        attackInterval=Timing(2.7f,1.9f); nextAttack=1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        for(int s=-1;s<=1;s+=2) for(int j=0;j<Shots(2,3);j++)
            Fire<SimpleRound>(Muzzle(s<0?0:1),CombatVisuals.Rotate(Vector2.down,s*(17+j*18)),2.5f+j*.2f,.24f);
    }
    
}

public static partial class WavePartLibrary
{
    static readonly FlightCurve DuneMothRoute0=Route("Desert / DuneMoth 0", Vector2.right, -.72f,1.25f,-.52f,.93f,-.31f,.62f,.2f,.57f,.62f,.8f,.7f,1.25f);
    static readonly FlightCurve DuneMothRoute1=Route("Desert / DuneMoth 1", Vector2.right, -1.4f,.69f,-.7f,.53f,0,.61f,.67f,.79f,1.4f,.7f);
    public static readonly WavePart[] DuneMothParts={
        new WavePart("DuneMoth call",nameof(DuneMoth),DuneMothRoute0,WaveShape.Rake,3,4.75f,2, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
        new WavePart("DuneMoth crossing",nameof(DuneMoth),DuneMothRoute1,WaveShape.Rake,3,5.00f,4, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
        new WavePart("DuneMoth answer",nameof(DuneMoth),DuneMothRoute0,WaveShape.Train,3,5.25f,7, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedDuneMoth;
    public static Mesh DuneMothMesh()
    {
        if(cachedDuneMoth!=null) return cachedDuneMoth;
        var b=new Builder();
        BevelArmor(b,new[]{V(0,-.83f),V(-.16f,-.2f),V(-.18f,.71f),V(.18f,.71f),V(.16f,-.2f)},.28f,13);
        foreach(int s in new[]{-1,1}) {
            Armor(b,new[]{V(s*.14f,.42f),V(s*1.2f,.67f),V(s*.92f,-.27f),V(s*.43f,-.08f)},.1f,15);
            b.Prism(new[]{V(s*.27f,.35f),V(s*.98f,.48f),V(s*.71f,-.12f)},.07f,13,Vector3.back*.13f,6);
            Barrel(b,s*.72f,-.1f,.34f,.14f,-.16f); Motor(b,s*.29f,.68f,.18f,.29f,13);
        }
        Glass(b,0,-.12f,.2f,.35f,-.28f);
        return cachedDuneMoth=b.Finish("DuneMoth authored hull and weapon lips");
    }
}
