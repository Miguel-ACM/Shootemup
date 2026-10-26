using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class StormOrgan : DistrictEnemy
{
    protected override void Configure()
    {
        hull=70; hullScale=0.57f; health=48;
        attackInterval=Timing(2.7f,1.9f); nextAttack=1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        var local=EnemyWeaponLayout.Ports(hull)[volley%2];
        var beam=Fire<PulseLaser>(Hull.TransformPoint(local),Vector2.down,0);
        if(beam!=null) { beam.Bind(Hull,local); beam.warning=Timing(1.3f,1.05f); beam.activeTime=Timing(.65f,.95f); beam.width=.22f; }
    }
    
    protected override void PrepareFire(float seconds) { base.PrepareFire(seconds); firingCue.ClearWarning(); Cue(Muzzle((volley+1)%2),seconds); }

}

public static partial class WavePartLibrary
{
    static readonly FlightCurve StormOrganRoute0=Route("Thunder / StormOrgan 0", Vector2.right, -.65f,1.25f,-.57f,.95f,-.3f,.78f,.23f,.78f,.57f,.96f,.65f,1.25f);
    static readonly FlightCurve StormOrganRoute1=Route("Thunder / StormOrgan 1", Vector2.right, -1.4f,.88f,-.8f,.85f,-.15f,.82f,.55f,.87f,1.4f,.91f);
    public static readonly WavePart[] StormOrganParts={
        new WavePart("StormOrgan call",nameof(StormOrgan),StormOrganRoute0,WaveShape.Train,2,6.15f,2, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 2),
        new WavePart("StormOrgan crossing",nameof(StormOrgan),StormOrganRoute1,WaveShape.Train,2,6.40f,4, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 2),
        new WavePart("StormOrgan answer",nameof(StormOrgan),StormOrganRoute0,WaveShape.Rake,2,6.65f,7, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 2),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedStormOrgan;
    public static Mesh StormOrganMesh()
    {
        if(cachedStormOrgan!=null) return cachedStormOrgan;
        var b=new Builder();
        b.Box(new Vector3(0,.15f,0),new Vector3(.9f,.55f,.27f),7,6);
        foreach(int s in new[]{-1,1}) {
            BevelArmor(b,new[]{V(s*.2f,-.82f),V(s*.55f,-.82f),V(s*.62f,.86f),V(s*.3f,1.04f)},.32f,15);
            Barrel(b,s*.38f,-.6f,.84f,.2f,-.23f);
            b.Drum(new Vector3(s*.38f,.1f,-.3f),.18f,.16f,10,8,11);
            Motor(b,s*.38f,.82f,.22f,.3f,7);
        }
        b.Box(new Vector3(0,.52f,-.12f),new Vector3(1.48f,.09f,.13f),6,11);
        return cachedStormOrgan=b.Finish("StormOrgan authored hull and weapon lips");
    }
}
