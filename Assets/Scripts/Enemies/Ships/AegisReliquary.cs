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
        b.Pearl(new Vector3(0,.12f,.04f),new Vector3(.77f,.95f,.38f),7);
        for(int s=-1;s<=1;s+=2)
        {
            b.Sweep(new Vector3(0,.96f,-.1f),new Vector3(s*1.1f,.85f,-.1f),new Vector3(s*.9f,-.52f,-.23f),new Vector3(0,-1.08f,-.06f),.32f,.28f,15);
            b.Sweep(new Vector3(0,.83f,-.3f),new Vector3(s*.72f,.72f,-.43f),new Vector3(s*.65f,-.38f,-.42f),new Vector3(0,-.87f,-.24f),.035f,.035f,3,false);
            Feather(b,new Vector3(s*.6f,.27f,.06f),new Vector3(s*1.12f,.88f,.12f),.23f,s*.13f);
            b.Pearl(new Vector3(s*.72f,-.4f,-.10f),new Vector3(.29f,.58f,.27f),1);
            CelestialMuzzle(b,new Vector3(s*.72f,-.96f,-.25f),.19f);
        }
        b.Pearl(new Vector3(0,.14f,-.37f),new Vector3(.28f,.46f,.12f),11);
        b.Pearl(new Vector3(0,.18f,-.48f),new Vector3(.14f,.31f,.10f),8);
        return aegisReliquary=b.Finish("Aegis / curved shield shells and twin vaulted batteries");
    }
}
