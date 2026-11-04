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
        BevelArmor(b,new[]{V(0,-1.45f),V(-.24f,-.52f),V(-.2f,.72f),V(0,.96f),V(.2f,.72f),V(.24f,-.52f)},.25f,15);
        for(int s=-1;s<=1;s+=2)
        {
            b.Prism(new[]{V(s*.15f,.5f),V(s*.84f,.95f),V(s*.70f,.32f),V(s*.24f,-.5f)},.13f,3,Vector3.zero,7);
            Barrel(b,s*.24f,-.55f,.57f,.1f,-.17f);
            b.Prism(new[]{V(s*.2f,.6f),V(s*.47f,.85f),V(s*.38f,1.15f)},.07f,1,new Vector3(0,0,-.12f),2);
        }
        b.Drum(new Vector3(0,.14f,-.18f),.16f,.10f,9,8,7);
        return votiveSpear=b.Finish("Votive spear / elongated lance and swept pennant wings");
    }
}
