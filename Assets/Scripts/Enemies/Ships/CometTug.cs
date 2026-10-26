using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class CometTug : DistrictEnemy
{
    protected override void Configure()
    {
        hull=76; hullScale=0.55f; health=47;
        attackInterval=Timing(2.7f,1.9f); nextAttack=1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        int n=Shots(3,5);
        for(int i=0;i<n;i++) Fire<SuspendedRound>(Muzzle(i),CombatVisuals.Rotate(Vector2.down,(i-(n-1)*.5f)*22+(volley%2==0?8:-8)),3.2f,.23f);
    }
    
}

public static partial class WavePartLibrary
{
    static readonly FlightCurve CometTugRoute0=Route("Space / CometTug 0", Vector2.right, .68f,1.25f,.61f,.93f,.31f,.71f,-.18f,.66f,-.55f,.86f,-.65f,1.25f);
    static readonly FlightCurve CometTugRoute1=Route("Space / CometTug 1", Vector2.right, -.68f,1.25f,-.53f,.89f,-.1f,.61f,.45f,.74f,.65f,.96f,.68f,1.25f);
    public static readonly WavePart[] CometTugParts={
        new WavePart("CometTug call",nameof(CometTug),CometTugRoute0,WaveShape.Train,3,5.55f,2, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
        new WavePart("CometTug crossing",nameof(CometTug),CometTugRoute1,WaveShape.Train,3,5.80f,4, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
        new WavePart("CometTug answer",nameof(CometTug),CometTugRoute0,WaveShape.Rake,3,6.05f,7, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedCometTug;
    public static Mesh CometTugMesh()
    {
        if(cachedCometTug!=null) return cachedCometTug;
        var b=new Builder();
        BevelArmor(b,new[]{V(-.28f,-.6f),V(.28f,-.6f),V(.38f,.5f),V(-.18f,.8f),V(-.38f,.4f)},.38f,15);
        b.Box(new Vector3(-.45f,-.26f,0),new Vector3(.24f,1.44f,.24f),13,6);
        b.Box(new Vector3(.68f,-.1f,0),new Vector3(.23f,1.22f,.23f),13,6);
        b.Box(new Vector3(.2f,.19f,.06f),new Vector3(1.9f,.1f,.14f),15,6);
        SolarPanel(b,new Vector3(-.95f,.2f,.08f),.49f,1.2f);
        SpaceDish(b,new Vector3(.88f,.59f,-.1f),.28f);
        Barrel(b,-.45f,-.75f,.46f,.18f,-.2f); Barrel(b,.68f,-.49f,.44f,.18f,-.2f);
        b.Drum(new Vector3(0,.13f,-.3f),.23f,.17f,11,8,13);
        return cachedCometTug=b.Finish("CometTug authored hull and weapon lips");
    }
}
