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
            HeavenArmory.Lance(Hull,EnemyWeaponLayout.Ports(hull)[side<0?0:1],CombatVisuals.Rotate(Vector2.down,side*28),.74f,1.5f,1.4f);
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
        BevelArmor(b,new[]{V(0,-.86f),V(-.44f,0),V(0,1.05f),V(.44f,0)},.5f,9);
        for(int s=-1;s<=1;s+=2)
        {
            b.Prism(new[]{V(s*.26f,.6f),V(s*1.05f,.3f),V(s*.78f,-.85f),V(s*.5f,-.55f)},.24f,3,Vector3.zero,7);
            b.Drum(new Vector3(s*.68f,-.6f,-.18f),.21f,.19f,11,8,2);
            b.Drum(new Vector3(s*.68f,-.6f,-.3f),.13f,.04f,8,8,3);
        }
        b.Prism(new[]{V(-.13f,.6f),V(0,1.2f),V(.13f,.6f)},.2f,8,new Vector3(0,0,-.1f),7);
        return prismCantor=b.Finish("Prism cantor / diamond crystal between open golden forks");
    }
}
