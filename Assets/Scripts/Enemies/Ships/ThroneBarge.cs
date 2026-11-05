using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class ThroneBarge : DistrictEnemy
{
    protected override Vector2 CueAxis=>Vector2.down;
    protected override void Configure(){hullScale=.79f;health=110;attackInterval=4.7f;nextAttack=1.7f;Mover<Slalom>();}
    protected override void Attack()
    {
        HeavenArmory.Lance(Hull,EnemyWeaponLayout.Ports(hull)[0],Vector2.down,1.6f,1.1f,1.65f);
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
        CutStone(b,new Vector3(0,.02f,.09f),new Vector3(1.08f,.9f,.44f),7);
        for(int s=-1;s<=1;s+=2)
        {
            CutCurve(b,new Vector3(s*.18f,-.7f,-.08f),new Vector3(s*1.5f,-.7f,-.16f),new Vector3(s*1.46f,.72f,-.14f),new Vector3(s*.68f,1.2f,0),.32f,.26f,15);
            for(int rib=0;rib<4;rib++)
            {
                float x=s*(.36f+rib*.26f),top=1.48f-rib*.16f;
                CutCurve(b,new Vector3(x,-.35f,-.20f),new Vector3(x*.85f,.28f,-.40f),new Vector3(x,top-.2f,-.12f),new Vector3(x*1.1f,top,.10f),.09f,.11f,rib%2==0?3:15);
            }
            CutStone(b,new Vector3(s*.9f,-.45f,-.12f),new Vector3(.30f,.4f,.29f),1);
            CelestialMuzzle(b,new Vector3(s*.9f,-.87f,-.27f),.18f);
        }
        CutHalo(b,new Vector3(0,-.14f,-.44f),.46f,.09f,new Vector3(1,1.15f,1),Quaternion.identity,3);
        CutStone(b,new Vector3(0,-.14f,-.46f),new Vector3(.31f,.36f,.13f),11);
        CutStone(b,new Vector3(0,-.14f,-.58f),new Vector3(.18f,.23f,.07f),8);
        CelestialMuzzle(b,new Vector3(0,-1.03f,-.23f),.28f);
        return throneBarge=b.Finish("Throne / sculpted cathedral crown and siege aperture");
    }
}
