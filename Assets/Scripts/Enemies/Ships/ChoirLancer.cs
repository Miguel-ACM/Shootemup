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
        CelestialKeel(b,.33f,2.0f,.28f);
        for(int s=-1;s<=1;s+=2)
        {
            b.Sweep(new Vector3(s*.16f,.45f,0),new Vector3(s*.7f,1.04f,-.18f),new Vector3(s*1.2f,.85f,-.13f),new Vector3(s*1.48f,.56f,.02f),.19f,.14f,7);
            for(int f=0;f<5;f++)Feather(b,new Vector3(s*(.25f+f*.13f),.58f-f*.08f,-.05f),new Vector3(s*(1.55f-f*.13f),.63f-f*.39f,.06f),.22f,-s*.15f,f%2==0?15:1);
        }
        b.Pearl(new Vector3(0,.23f,-.29f),new Vector3(.17f,.43f,.12f),9);
        CelestialMuzzle(b,new Vector3(0,-1,-.23f),.16f);
        return choirLancer=b.Finish("Choir / sculpted ivory pinions and recessed lance");
    }
}
