using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class ChoirLancer : DistrictEnemy
{
    protected override Vector2 CueAxis => Vector2.down;
    protected override void Configure(){hullScale=.52f;health=38;attackInterval=3.2f;nextAttack=1.3f;Mover<Slalom>();}
    protected override void Attack(){HeavenArmory.Lance(Hull,EnemyWeaponLayout.Ports(hull)[0],Vector2.down,.62f,1.15f);}
}
public static partial class WavePartLibrary
{
    public static readonly WavePart[] ChoirLancerParts={
        new WavePart("Choir / parted nave",nameof(ChoirLancer),Route("Heaven / nave",Vector2.right,-.55f,1.3f,-.57f,.9f,-.52f,.77f,-.44f,.70f,-.66f,.55f,-1.5f,.42f),WaveShape.Train,3,8.2f,2,new[]{.25f,.52f}),
        new WavePart("Choir / vaulted crossing",nameof(ChoirLancer),Route("Heaven / vault",Vector2.up,-1.5f,.85f,-.65f,.81f,0,.76f,.65f,.81f,1.5f,.85f),WaveShape.Train,3,7.8f,2,new[]{.25f,.57f}),
        new WavePart("Choir / descending aisle",nameof(ChoirLancer),Route("Heaven / aisle",Vector2.right,-.55f,1.3f,-.62f,.86f,-.55f,.62f,-.60f,.4f,-1.5f,.22f),WaveShape.Rank,2,7.6f,5,new[]{.26f,.55f})};
}
public static partial class IndustrialMeshFactory
{
    static Mesh choirLancer;
    public static Mesh ChoirLancerMesh()
    {
        if(choirLancer!=null)return choirLancer;var b=new Builder();
        BevelArmor(b,new[]{V(0,-1.05f),V(-.22f,-.5f),V(-.28f,.75f),V(0,1),V(.28f,.75f),V(.22f,-.5f)},.30f,15);
        for(int side=-1;side<=1;side+=2)for(int feather=0;feather<3;feather++)
        {
            float x=.23f+feather*.22f;
            b.Prism(new[]{V(side*x,.65f),V(side*(x+.34f),.95f),V(side*(x+.45f),.55f),V(side*(x+.17f),-.55f)},.13f,feather%2==0?1:3,new Vector3(0,0,-.06f),7);
        }
        b.Drum(new Vector3(0,-.45f,-.3f),.25f,.18f,7,12,2);b.Drum(new Vector3(0,-.45f,-.41f),.16f,.04f,8,12,11);
        Barrel(b,0,-.65f,.7f,.18f,-.23f);
        return choirLancer=b.Finish("Choir lancer / layered feather wings and radiant lance");
    }
}
