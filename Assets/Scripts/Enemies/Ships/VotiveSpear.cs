using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class VotiveSpear : DistrictEnemy
{
    protected override void Configure(){hullScale=.43f;health=24;attackInterval=1.6f;nextAttack=.8f;Mover<Slalom>();}
    protected override void Attack()
    {
        int count=Dense(5);
        // A staggered stack separates along one committed aim, then bends out into feathers.
        for(int i=0;i<count;i++)for(int s=-1;s<=1;s+=2)
            Fire<NeedleRound>(Muzzle(s<0?0:1),CombatVisuals.Rotate(Aim,s*(5+i*3)),2.1f+i*.48f,.16f).Ramp(1.45f,.7f,.5f);
    }
}
public static partial class WavePartLibrary
{
    public static readonly WavePart[] VotiveSpearParts={
        new WavePart("Spear / angelic scissor",nameof(VotiveSpear),Route("Heaven / scissor",Vector2.up,-1.5f,.90f,-.7f,.80f,0,.54f,.6f,.31f,1.5f,.30f),WaveShape.Train,4,5.5f,2,new[]{.27f,.42f,.60f}),
        new WavePart("Spear / pennant dive",nameof(VotiveSpear),Route("Heaven / pennant",Vector2.right,-.55f,1.3f,-.5f,.8f,-.1f,.4f,.55f,.34f,1.5f,.55f),WaveShape.Chevron,3,6.1f,2,new[]{.28f,.46f,.64f}),
        new WavePart("Spear / returning guard",nameof(VotiveSpear),Route("Heaven / return",Vector2.up,-1.5f,.54f,-.55f,.60f,.25f,.77f,.65f,.92f,.25f,1.4f),WaveShape.Train,4,6.2f,4,new[]{.26f,.44f,.62f})};
}
public static partial class IndustrialMeshFactory
{
    static Mesh votiveSpear;
    public static Mesh VotiveSpearMesh()
    {
        if(votiveSpear!=null)return votiveSpear;var b=new Builder();
        CelestialKeel(b,.23f,2.8f,.22f);
        for(int s=-1;s<=1;s+=2)
        {
            Feather(b,new Vector3(s*.12f,-.2f,0),new Vector3(s*.97f,1.08f,.15f),.25f,-s*.20f);
            Feather(b,new Vector3(s*.12f,.30f,-.03f),new Vector3(s*.45f,1.35f,.1f),.13f,-s*.09f,3);
            CutCurve(b,new Vector3(s*.1f,.35f,-.14f),new Vector3(s*.37f,.1f,-.25f),new Vector3(s*.25f,-.55f,-.24f),new Vector3(s*.24f,-.835f,-.17f),.08f,.09f,7,false,16);
            CelestialMuzzle(b,new Vector3(s*.24f,-.835f,-.17f),.085f);
        }
        CutStone(b,new Vector3(0,.21f,-.22f),new Vector3(.10f,.34f,.07f),9);
        return votiveSpear=b.Finish("Votive / needle prow and curved pennant blades");
    }
}
