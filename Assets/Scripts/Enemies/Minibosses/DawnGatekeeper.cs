using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class DawnGatekeeper : DistrictMiniboss
{
    protected override Vector2 CueAxis=>Vector2.down;
    protected override void Configure(){hullScale=.92f;health=740;attackInterval=4.2f;nextAttack=1.2f;}
    protected override Vector3 Position(float t)=>new Vector3(Mathf.Sin(t*.52f)*1.65f,Camera.UpLimit-3.0f+Mathf.Sin(t*.9f)*.3f,1);
    protected override void Attack()
    {
        var ports=EnemyWeaponLayout.Ports(hull);
        if(volley%3==1)
        {
            for(int i=0;i<2;i++)HeavenArmory.Lance(Hull,ports[i],Vector2.down,1.0f,2.1f,1.35f);
            Stream(Dense(8),.20f,n=>MirrorFan(Muzzle(2),Vector2.down,Dense(7),12,2.0f+n*.10f));
        }
        else if(volley%3==2)
        {
            for(int i=0;i<2;i++)HeavenArmory.Lance(Hull,ports[i],CombatVisuals.Rotate(Vector2.down,i==0?-34:34),1.25f,1.7f,1.4f);
            Stream(Dense(9),.17f,n=>{for(int s=-1;s<=1;s+=2)Fire<NeedleRound>(Muzzle(s<0?0:1),CombatVisuals.Rotate(Vector2.up,s*(20+n*6)),2.8f,.19f).Bounce(1);});
        }
        else
        {
            HeavenArmory.Lance(Hull,ports[2],Vector2.down,2.1f,1.9f,1.6f);
            Stream(Dense(8),.18f,n=>{for(int s=-1;s<=1;s+=2)MirrorFan(Muzzle(s<0?0:1),CombatVisuals.Rotate(Vector2.down,s*(45-n*4)),Dense(5),8,2.4f);});
        }
    }
}
public static partial class IndustrialMeshFactory
{
    static Mesh dawnGatekeeper;
    public static Mesh DawnGatekeeperMesh()
    {
        if(dawnGatekeeper!=null)return dawnGatekeeper;var b=new Builder();
        BevelArmor(b,new[]{V(-.6f,-.75f),V(-.72f,.65f),V(0,1.1f),V(.72f,.65f),V(.6f,-.75f)},.55f,15);
        for(int s=-1;s<=1;s+=2)
        {
            b.Prism(new[]{V(s*.5f,-.55f),V(s*1.7f,-.75f),V(s*1.5f,.8f),V(s*.7f,1.2f)},.32f,1,Vector3.zero,7);
            b.Box(new Vector3(s*1.25f,-.5f,-.26f),new Vector3(.4f,.9f,.42f),7,2);Barrel(b,s*1.25f,-.8f,.45f,.3f,-.28f);
            for(int f=0;f<3;f++)b.Prism(new[]{V(s*(.85f+f*.2f),.4f),V(s*(1.3f+f*.25f),1.32f),V(s*(1.45f+f*.25f),.65f)},.15f,3,new Vector3(0,0,-.12f),7);
        }
        b.Drum(new Vector3(0,-.3f,-.4f),.45f,.18f,7,12,2);b.Drum(new Vector3(0,-.3f,-.52f),.31f,.06f,8,12,11);
        Barrel(b,0,-.75f,.4f,.4f,-.25f);
        return dawnGatekeeper=b.Finish("Dawn gatekeeper / winged arch and three lance batteries");
    }
}
