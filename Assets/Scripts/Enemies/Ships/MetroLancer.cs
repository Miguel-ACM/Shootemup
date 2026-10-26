using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class MetroLancer : DistrictEnemy
{
    protected override void Configure()
    {
        hull=65; hullScale=0.41f; health=19;
        attackInterval=Timing(2.7f,1.9f); nextAttack=1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        Fan<NeedleRound>(Shots(2,3),11,5.4f,.17f);
    }
    
}

public static partial class WavePartLibrary
{
    static readonly FlightCurve MetroLancerRoute0=Route("City / MetroLancer 0", Vector2.right, -.8f,1.25f,-.62f,.94f,-.2f,.67f,.4f,.52f,.92f,.45f,1.4f,.6f);
    static readonly FlightCurve MetroLancerRoute1=Route("City / MetroLancer 1", Vector2.right, -1.4f,.9f,-.72f,.82f,-.07f,.58f,.58f,.73f,1.4f,.91f);
    public static readonly WavePart[] MetroLancerParts={
        new WavePart("MetroLancer call",nameof(MetroLancer),MetroLancerRoute0,WaveShape.Train,4,2.75f,2, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
        new WavePart("MetroLancer crossing",nameof(MetroLancer),MetroLancerRoute1,WaveShape.Train,4,3.00f,4, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
        new WavePart("MetroLancer answer",nameof(MetroLancer),MetroLancerRoute0,WaveShape.Rake,4,3.25f,7, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedMetroLancer;
    public static Mesh MetroLancerMesh()
    {
        if(cachedMetroLancer!=null) return cachedMetroLancer;
        var b=new Builder();
        BevelArmor(b,new[]{V(0,-1.14f),V(-.2f,-.6f),V(-.24f,.86f),V(.24f,.86f),V(.2f,-.6f)},.32f,13);
        foreach(int s in new[]{-1,1}) {
            b.Box(new Vector3(s*.2f,-.53f,-.15f),new Vector3(.1f,.98f,.15f),11,6);
            Armor(b,new[]{V(s*.17f,.12f),V(s*.77f,.67f),V(s*.45f,.55f),V(s*.28f,-.18f)},.12f,3);
            Motor(b,s*.24f,.74f,.18f,.32f,13);
        }
        Glass(b,0,.09f,.25f,.52f,-.3f);
        return cachedMetroLancer=b.Finish("MetroLancer authored hull and weapon lips");
    }
}
