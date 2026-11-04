using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class ThroneBarge : DistrictEnemy
{
    protected override Vector2 CueAxis=>Vector2.down;
    protected override void Configure(){hullScale=.79f;health=110;attackInterval=4.7f;nextAttack=1.7f;Mover<Slalom>();}
    protected override void Attack()
    {
        HeavenArmory.Lance(Hull,EnemyWeaponLayout.Ports(hull)[0],Vector2.down,1.6f,2.0f,1.65f);
        int n=Dense(9);
        for(int s=-1;s<=1;s+=2)for(int i=0;i<n;i++)
            Fire<NeedleRound>(Muzzle(s<0?1:2),CombatVisuals.Rotate(Vector2.down,s*(30+i*10)),2.2f,.19f).Bounce(1).FadeAt(8);
    }
}
public static partial class WavePartLibrary
{
    public static readonly WavePart[] ThroneBargeParts={
        new WavePart("Throne / siege terrace",nameof(ThroneBarge),Route("Heaven / siege terrace",Vector2.right,-.38f,1.5f,-.4f,.9f,-.35f,.83f,-.3f,.76f,-.55f,.67f,-1.7f,.6f),WaveShape.Solo,1,11.0f,3,new[]{.28f,.65f}),
        new WavePart("Throne / royal crossing",nameof(ThroneBarge),Route("Heaven / royal crossing",Vector2.up,-1.7f,.92f,-.65f,.86f,0,.82f,.65f,.86f,1.7f,.92f),WaveShape.Solo,1,10.8f,6,new[]{.25f,.63f}),
        new WavePart("Throne / broken crown",nameof(ThroneBarge),Route("Heaven / broken crown",Vector2.right,.45f,1.5f,.50f,.9f,.55f,.79f,.52f,.69f,.67f,.59f,1.7f,.45f),WaveShape.Solo,1,11.3f,9,new[]{.27f,.64f})};
}
public static partial class IndustrialMeshFactory
{
    static Mesh throneBarge;
    public static Mesh ThroneBargeMesh()
    {
        if(throneBarge!=null)return throneBarge;var b=new Builder();
        BevelArmor(b,new[]{V(-1.15f,-.62f),V(-1.3f,.58f),V(-.75f,1),V(.75f,1),V(1.3f,.58f),V(1.15f,-.62f)},.55f,15);
        for(int s=-1;s<=1;s+=2)
        {
            for(int i=0;i<3;i++)b.Prism(new[]{V(s*(.55f+i*.25f),.22f),V(s*(.65f+i*.25f),1.1f+i*.16f),V(s*(.85f+i*.25f),.35f)},.22f,3,new Vector3(0,0,-.3f),7);
            b.Box(new Vector3(s*.9f,-.42f,-.25f),new Vector3(.4f,.65f,.36f),1,7);Barrel(b,s*.9f,-.70f,.34f,.22f,-.27f);
        }
        b.Drum(new Vector3(0,-.1f,-.35f),.54f,.28f,7,12,2);
        b.Drum(new Vector3(0,-.1f,-.51f),.36f,.08f,8,12,11);
        b.Box(new Vector3(0,-.68f,-.18f),new Vector3(.65f,.55f,.42f),7,2);
        Barrel(b,0,-.84f,.38f,.48f,-.23f);
        return throneBarge=b.Finish("Throne barge / crown fins and cathedral siege mouth");
    }
}
