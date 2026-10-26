using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class RivetMantis : DistrictEnemy
{
    protected override Vector2 CueAxis=>Vector2.down;
    protected override void Configure()
    {
        hull=68; hullScale=0.55f; health=40;
        attackInterval=Timing(2.7f,1.9f); nextAttack=1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        Fan<RicochetBolt>(Shots(3,5),25,3.1f,.21f);
    }
    
}

public static partial class WavePartLibrary
{
    static readonly FlightCurve RivetMantisRoute0=Route("Tunnel / RivetMantis 0", Vector2.right, .68f,1.25f,.55f,.93f,.2f,.7f,-.22f,.56f,-.6f,.72f,-.6f,1.25f);
    static readonly FlightCurve RivetMantisRoute1=Route("Tunnel / RivetMantis 1", Vector2.right, -.35f,1.25f,-.2f,.87f,.37f,.7f,.58f,.36f,.2f,.06f,-.3f,-.25f);
    public static readonly WavePart[] RivetMantisParts={
        new WavePart("RivetMantis call",nameof(RivetMantis),RivetMantisRoute0,WaveShape.Train,3,5.25f,2, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
        new WavePart("RivetMantis crossing",nameof(RivetMantis),RivetMantisRoute1,WaveShape.Train,3,5.50f,4, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
        new WavePart("RivetMantis answer",nameof(RivetMantis),RivetMantisRoute0,WaveShape.Rake,3,5.75f,7, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedRivetMantis;
    public static Mesh RivetMantisMesh()
    {
        if(cachedRivetMantis!=null) return cachedRivetMantis;
        var b=new Builder();
        BevelArmor(b,new[]{V(0,-.8f),V(-.27f,-.3f),V(-.2f,.74f),V(.2f,.74f),V(.27f,-.3f)},.32f,6);
        foreach(int s in new[]{-1,1}) {
            Armor(b,new[]{V(s*.17f,.3f),V(s*.98f,.58f),V(s*1.1f,-.24f),V(s*.78f,-.83f),V(s*.65f,-.75f),V(s*.76f,-.08f),V(s*.39f,.08f)},.18f,0);
            Barrel(b,s*.78f,-.55f,.44f,.15f,-.16f); Motor(b,s*.3f,.69f,.19f,.3f,6);
        }
        Glass(b,0,-.09f,.25f,.35f,-.31f);
        return cachedRivetMantis=b.Finish("RivetMantis authored hull and weapon lips");
    }
}
