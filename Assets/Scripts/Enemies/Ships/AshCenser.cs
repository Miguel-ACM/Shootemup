using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class AshCenser : DistrictEnemy
{
    protected override void Configure()
    {
        hull=79; hullScale=0.58f; health=55;
        attackInterval=Timing(2.7f,1.9f); nextAttack=1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        for(int s=-1;s<=1;s+=2) Fire<PlasmaOrb>(Muzzle(s<0?0:1),CombatVisuals.Rotate(Vector2.down,s*(volley%2==0?32:15)),1.7f,.38f);
        if(!Difficulty.Easy) Fan<SimpleRound>(3,24,2.8f,.22f);
    }
    
}

public static partial class WavePartLibrary
{
    static readonly FlightCurve AshCenserRoute0=Route("Hell / AshCenser 0", Vector2.right, -.7f,1.25f,-.61f,.94f,-.33f,.71f,.21f,.61f,.59f,.81f,.7f,1.25f);
    static readonly FlightCurve AshCenserRoute1=Route("Hell / AshCenser 1", Vector2.right, -1.4f,.83f,-.82f,.71f,-.15f,.56f,.53f,.74f,1.4f,.85f);
    public static readonly WavePart[] AshCenserParts={
        new WavePart("AshCenser call",nameof(AshCenser),AshCenserRoute0,WaveShape.Rake,3,5.45f,2, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
        new WavePart("AshCenser crossing",nameof(AshCenser),AshCenserRoute1,WaveShape.Rake,3,5.70f,4, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
        new WavePart("AshCenser answer",nameof(AshCenser),AshCenserRoute0,WaveShape.Train,3,5.95f,7, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedAshCenser;
    public static Mesh AshCenserMesh()
    {
        if(cachedAshCenser!=null) return cachedAshCenser;
        var b=new Builder();
        b.Drum(Vector3.zero,.51f,.37f,6,8,11);
        b.Drum(new Vector3(0,-.2f,-.29f),.34f,.19f,10,8,13);
        for(int i=0;i<6;i++) {
            float a=i*Mathf.PI/3;
            b.Box(new Vector3(Mathf.Cos(a)*.45f,Mathf.Sin(a)*.45f,-.24f),new Vector3(.12f,.2f,.33f),13,6);
        }
        foreach(int s in new[]{-1,1}) {
            b.Prism(new[]{V(s*.17f,.39f),V(s*.5f,.99f),V(s*.13f,.76f)},.17f,15,Vector3.zero,6);
            Barrel(b,s*.41f,-.33f,.38f,.17f,-.24f);
        }
        b.Prism(new[]{V(-.24f,-.41f),V(0,-.94f),V(.24f,-.41f)},.2f,13,Vector3.zero,6);
        return cachedAshCenser=b.Finish("AshCenser authored hull and weapon lips");
    }
}
