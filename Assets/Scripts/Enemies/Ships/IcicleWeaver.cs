using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class IcicleWeaver : DistrictEnemy
{
    protected override void Configure()
    {
        hull=63; hullScale=0.49f; health=27;
        attackInterval=Timing(2.7f,1.9f); nextAttack=1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        int count=Shots(3,5);
        for(int i=0;i<count;i++)
            Fire<SuspendedRound>(Muzzle(i),CombatVisuals.Rotate(Vector2.down,(i-(count-1)*.5f)*19+(volley%2==0?8:-8)),2.9f,.19f);
    }
    
}

public static partial class WavePartLibrary
{
    static readonly FlightCurve IcicleWeaverRoute0=Route("Snow / IcicleWeaver 0", Vector2.right, -.65f,1.25f,-.5f,.93f,-.2f,.69f,.23f,.56f,.61f,.69f,.62f,1.25f);
    static readonly FlightCurve IcicleWeaverRoute1=Route("Snow / IcicleWeaver 1", Vector2.right, -1.4f,.77f,-.82f,.68f,-.2f,.8f,.45f,.66f,1.4f,.76f);
    public static readonly WavePart[] IcicleWeaverParts={
        new WavePart("IcicleWeaver call",nameof(IcicleWeaver),IcicleWeaverRoute0,WaveShape.Rake,3,5.15f,2, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
        new WavePart("IcicleWeaver crossing",nameof(IcicleWeaver),IcicleWeaverRoute1,WaveShape.Rake,3,5.40f,4, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
        new WavePart("IcicleWeaver answer",nameof(IcicleWeaver),IcicleWeaverRoute0,WaveShape.Train,3,5.65f,7, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedIcicleWeaver;
    public static Mesh IcicleWeaverMesh()
    {
        if(cachedIcicleWeaver!=null) return cachedIcicleWeaver;
        var b=new Builder();
        BevelArmor(b,new[]{V(-.2f,-.45f),V(0,-.77f),V(.2f,-.45f),V(.25f,.68f),V(-.25f,.68f)},.3f,7);
        foreach(int s in new[]{-1,1}) {
            Armor(b,new[]{V(s*.15f,.12f),V(s*.85f,.73f),V(s*.62f,-.73f),V(s*.46f,-.78f),V(s*.38f,-.06f)},.16f,15);
            Barrel(b,s*.55f,-.49f,.57f,.13f,-.18f); Motor(b,s*.37f,.59f,.18f,.28f,7);
        }
        Glass(b,0,.12f,.27f,.4f,-.28f);
        return cachedIcicleWeaver=b.Finish("IcicleWeaver authored hull and weapon lips");
    }
}
