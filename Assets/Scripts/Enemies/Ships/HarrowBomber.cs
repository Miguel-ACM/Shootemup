using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class HarrowBomber : DistrictEnemy
{
    protected override void Configure()
    {
        hull=62; hullScale=0.61f; health=48;
        attackInterval=Timing(2.7f,1.9f); nextAttack=1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        int count = Shots(3, 5);
        for (int i = 0; i < count; i++)
            Fire<SimpleRound>(Muzzle(i), CombatVisuals.Rotate(Vector2.down, (i-(count-1)*.5f)*24 + (volley%2==0?8:-8)), 2.1f + Mathf.Abs(i-(count-1)*.5f)*.18f, .29f);
    }
    
}

public static partial class WavePartLibrary
{
    static readonly FlightCurve HarrowBomberRoute0=Route("Fields / HarrowBomber 0", Vector2.right, .2f,1.25f,.4f,.92f,.57f,.74f,.35f,.58f,-.08f,.5f,-.52f,.68f,-.75f,1.25f);
    static readonly FlightCurve HarrowBomberRoute1=Route("Fields / HarrowBomber 1", Vector2.right, -.7f,1.25f,-.68f,.92f,-.35f,.7f,.25f,.69f,.7f,.9f,.68f,1.25f);
    public static readonly WavePart[] HarrowBomberParts={
        new WavePart("HarrowBomber call",nameof(HarrowBomber),HarrowBomberRoute0,WaveShape.Train,2,5.75f,2, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 2),
        new WavePart("HarrowBomber crossing",nameof(HarrowBomber),HarrowBomberRoute1,WaveShape.Train,2,6.00f,4, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 2),
        new WavePart("HarrowBomber answer",nameof(HarrowBomber),HarrowBomberRoute0,WaveShape.Rake,2,6.25f,7, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 2),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedHarrowBomber;
    public static Mesh HarrowBomberMesh()
    {
        if(cachedHarrowBomber!=null) return cachedHarrowBomber;
        var b=new Builder();
        foreach (int s in new[] {-1,1}) {
            BevelArmor(b,new[] {V(s*.24f,-.48f),V(s*.53f,-.75f),V(s*.92f,-.55f),V(s*1.06f,.46f),V(s*.73f,.88f),V(s*.34f,.54f)},.32f,2);
            b.Box(new Vector3(s*.72f,-.31f,-.16f),new Vector3(.28f,.54f,.23f),11,6);
            Motor(b,s*.7f,.64f,.25f,.4f,0);
        }
        b.Box(new Vector3(0,.22f,0),new Vector3(1.12f,.25f,.25f),6,11);
        Glass(b,0,.21f,.4f,.25f,-.22f);
        return cachedHarrowBomber=b.Finish("HarrowBomber authored hull and weapon lips");
    }
}
