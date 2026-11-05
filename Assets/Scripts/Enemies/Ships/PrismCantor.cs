using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class PrismCantor : DistrictEnemy
{
    protected override Vector2 CueAxis=>Vector2.down;
    protected override void Configure(){hullScale=.56f;health=45;attackInterval=3.8f;nextAttack=1.4f;Mover<Slalom>();}
    protected override void Attack()
    {
        // Outward prismatic forks leave a navigable aisle under the central crystal.
        for(int side=-1;side<=1;side+=2)
            HeavenArmory.Lance(Hull,EnemyWeaponLayout.Ports(hull)[side<0?0:1],CombatVisuals.Rotate(Vector2.down,side*28),.74f,.85f,1.4f);
        for(int i=0;i<Dense(4);i++)Fire<NeedleRound>(Muzzle(2),Vector2.up,2.0f+i*.4f,.17f).Bounce(1).FadeAt(7);
    }
}
public static partial class WavePartLibrary
{
    public static readonly WavePart[] PrismCantorParts={
        new WavePart("Cantor / split transept",nameof(PrismCantor),Route("Heaven / transept",Vector2.right,0,1.4f,-.08f,.9f,-.15f,.77f,.1f,.62f,.55f,.55f,1.5f,.72f),WaveShape.Train,2,8.4f,2,new[]{.27f,.58f}),
        new WavePart("Cantor / flying buttress",nameof(PrismCantor),Route("Heaven / buttress",Vector2.up,-1.5f,.95f,-.6f,.87f,0,.75f,.6f,.87f,1.5f,.95f),WaveShape.Train,2,8.0f,3,new[]{.26f,.55f}),
        new WavePart("Cantor / rising psalm",nameof(PrismCantor),Route("Heaven / psalm",Vector2.up,-1.5f,.57f,-.65f,.65f,-.35f,.86f,.35f,.86f,.65f,.65f,1.5f,.57f),WaveShape.Train,2,8.8f,7,new[]{.25f,.57f})};
}
public static partial class IndustrialMeshFactory
{
    static Mesh prismCantor;
    public static Mesh PrismCantorMesh()
    {
        if(prismCantor!=null)return prismCantor;var b=new Builder();
        CutStone(b,new Vector3(0,0,-.1f),new Vector3(.43f,1.05f,.34f),9,6,Mathf.PI/6);
        for(int s=-1;s<=1;s+=2)
        {
            CutCurve(b,new Vector3(0,.83f,.1f),new Vector3(s*1.28f,.84f,.05f),new Vector3(s*1.23f,-.26f,-.1f),new Vector3(s*.68f,-.62f,-.23f),.19f,.15f,3);
            CutCurve(b,new Vector3(s*.18f,.48f,-.25f),new Vector3(s*.73f,.33f,-.4f),new Vector3(s*.8f,-.20f,-.4f),new Vector3(s*.68f,-.60f,-.3f),.11f,.12f,15);
            CutHalo(b,new Vector3(s*.68f,-.6f,-.26f),.22f,.055f,Vector3.one,Quaternion.identity,7,32);
            CutStone(b,new Vector3(s*.68f,-.6f,-.31f),new Vector3(.13f,.13f,.10f),8);
        }
        CutHalo(b,new Vector3(0,.05f,0),.72f,.026f,Vector3.one,Quaternion.Euler(58,0,0),3);
        CutStone(b,new Vector3(0,1.2f,-.1f),new Vector3(.09f,.16f,.09f),8);
        return prismCantor=b.Finish("Cantor / suspended crystal and arched prismatic cradle");
    }
}
