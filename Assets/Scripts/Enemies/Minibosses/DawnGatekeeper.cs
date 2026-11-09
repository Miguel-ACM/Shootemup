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
            for(int i=0;i<2;i++)HeavenArmory.Lance(Hull,ports[i],Vector2.down,1.0f,1.15f,1.35f);
            // Successive fans of the stream alternate bright and dark.
            Stream(Dense(8),.20f,n=>
            {
                UseTone(n%2==0?HostileTone.Bright:HostileTone.Dark);
                MirrorFan(Muzzle(2),Vector2.down,Dense(7),12,2.0f+n*.10f);
            });
        }
        else if(volley%3==2)
        {
            for(int i=0;i<2;i++)HeavenArmory.Lance(Hull,ports[i],CombatVisuals.Rotate(Vector2.down,i==0?-34:34),1.25f,.95f,1.4f);
            // The left wing's rebounding needles are bright, the right wing's dark.
            Stream(Dense(9),.17f,n=>
            {
                for(int s=-1;s<=1;s+=2)
                {
                    UseTone(s<0?HostileTone.Bright:HostileTone.Dark);
                    Fire<NeedleRound>(Muzzle(s<0?0:1),CombatVisuals.Rotate(Vector2.up,s*(20+n*6)),2.8f,.19f).Bounce(1);
                }
            });
        }
        else
        {
            HeavenArmory.Lance(Hull,ports[2],Vector2.down,2.1f,1.05f,1.6f);
            // Dark fans beneath the bright, unabsorbable central lance.
            Stream(Dense(8),.18f,n=>{UseTone(HostileTone.Dark);for(int s=-1;s<=1;s+=2)MirrorFan(Muzzle(s<0?0:1),CombatVisuals.Rotate(Vector2.down,s*(45-n*4)),Dense(5),8,2.4f);});
        }
    }
}
public static partial class IndustrialMeshFactory
{
    static Mesh dawnGatekeeper;
    public static Mesh DawnGatekeeperMesh()
    {
        if(dawnGatekeeper!=null)return dawnGatekeeper;var b=new Builder();
        CelestialKeel(b,.52f,2.25f,.39f);
        for(int s=-1;s<=1;s+=2)
        {
            CutBlade(b,new[]{new Vector3(s*.45f,-.45f,0),new Vector3(s*1.75f,-.72f,.07f),new Vector3(s*1.86f,.68f,.07f),new Vector3(s*.48f,1.5f,.05f)},.26f,.26f,7);
            for(int f=0;f<5;f++)Feather(b,new Vector3(s*(.54f+f*.17f),.62f-f*.11f,-.08f),new Vector3(s*(1.25f+f*.22f),1.58f-f*.43f,.09f),.25f,-s*.16f);
            CutBlade(b,new[]{new Vector3(s*.74f,.64f,0),new Vector3(s*1.50f,.38f,-.05f),new Vector3(s*1.43f,-.52f,-.12f),new Vector3(s*1.25f,-1.025f,-.28f)},.28f,.24f,15);
            CelestialMuzzle(b,new Vector3(s*1.25f,-1.025f,-.28f),.24f);
        }
        CutStone(b,new Vector3(0,.25f,-.32f),new Vector3(.44f,.48f,.16f),3,6);
        CutStone(b,new Vector3(0,.25f,-.44f),new Vector3(.31f,.34f,.10f),9,6);
        CutStone(b,new Vector3(0,.25f,-.55f),new Vector3(.10f,.19f,.025f),8,4);
        CelestialMuzzle(b,new Vector3(0,-.95f,-.25f),.24f);
        return dawnGatekeeper=b.Finish("Dawn / vaulted paladin with layered pinions and three siege mouths");
    }
}
