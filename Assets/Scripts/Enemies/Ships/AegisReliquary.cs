using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class AegisReliquary : DistrictEnemy
{
    protected override Vector2 CueAxis=>Vector2.down;
    protected override void Configure(){hullScale=.66f;health=74;attackInterval=3.7f;nextAttack=1.5f;Mover<Slalom>();}
    protected override void Attack()
    {
        var ports=EnemyWeaponLayout.Ports(hull);int side=volley%2;
        HeavenArmory.Lance(Hull,ports[side],CombatVisuals.Rotate(Vector2.down,side==0?-12:12),1.0f,1.5f,1.4f);
        int count=Dense(7);
        for(int i=0;i<count;i++)Fire<NeedleRound>(Muzzle(1-side),CombatVisuals.Rotate(Vector2.down,(i-(count-1)*.5f)*12),1.7f,.18f).Ramp(1.8f,.8f,.8f);
    }
}
public static partial class WavePartLibrary
{
    public static readonly WavePart[] AegisReliquaryParts={
        new WavePart("Reliquary / shield procession",nameof(AegisReliquary),Route("Heaven / shield procession",Vector2.right,-.48f,1.4f,-.52f,.9f,-.50f,.8f,-.45f,.68f,-.64f,.51f,-1.6f,.4f),WaveShape.Train,2,9.4f,2,new[]{.23f,.57f}),
        new WavePart("Reliquary / open gates",nameof(AegisReliquary),Route("Heaven / opening gates",Vector2.up,-1.5f,.93f,-.72f,.83f,-.42f,.75f,-.60f,.63f,-1.5f,.56f),WaveShape.Train,2,8.9f,4,new[]{.28f,.57f}),
        new WavePart("Reliquary / upper parapet",nameof(AegisReliquary),Route("Heaven / parapet",Vector2.up,-1.6f,.9f,-.68f,.86f,0,.82f,.68f,.86f,1.6f,.9f),WaveShape.Solo,1,8.4f,6,new[]{.24f,.58f})};
}
public static partial class IndustrialMeshFactory
{
    static Mesh aegisReliquary;
    public static Mesh AegisReliquaryMesh()
    {
        if(aegisReliquary!=null)return aegisReliquary;var b=new Builder();
        BevelArmor(b,new[]{V(0,-1.0f),V(-.82f,-.52f),V(-.88f,.70f),V(-.50f,1),V(.5f,1),V(.88f,.70f),V(.82f,-.52f)},.46f,1);
        BevelArmor(b,new[]{V(0,-.8f),V(-.57f,-.38f),V(-.6f,.62f),V(.6f,.62f),V(.57f,-.38f)},.12f,7,-.3f);
        b.Box(new Vector3(0,.1f,-.45f),new Vector3(.11f,1.25f,.08f),3,8);
        b.Box(new Vector3(0,.35f,-.45f),new Vector3(.9f,.11f,.08f),3,8);
        for(int s=-1;s<=1;s+=2){b.Box(new Vector3(s*.72f,-.38f,-.24f),new Vector3(.28f,.82f,.32f),15,7);Barrel(b,s*.72f,-.78f,.36f,.23f,-.25f);b.Drum(new Vector3(s*.72f,.3f,-.3f),.21f,.12f,8,8,2);}
        return aegisReliquary=b.Finish("Aegis reliquary / armored shield and twin siege batteries");
    }
}
