using System.Collections;
using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class MeadowTank : GroundVehicle
{
    bool bursting;
    protected override bool Snow=>false;
    protected override void Configure(){hull=39;hullScale=.68f;health=27;attackInterval=1.5f;}
    protected override void Attack(){if(Grounded && !bursting)StartCoroutine(Burst());}
    IEnumerator Burst()
    {
        bursting=true;Vector2 aim=CombatVisuals.Aim(Weapon.position);
        for(int i=0;i<2;i++)
        {
            var muzzle=Weapon.TransformPoint(new Vector3(0,-.96f,-.22f));
            if(player!=null && Vector2.Distance(player.position,muzzle)>2.2f)Fire<NeedleRound>(muzzle,CombatVisuals.Rotate(aim,i==0 ? -3 : 3),6.4f,.14f);
            yield return new WaitForSeconds(.12f/LevelRun.EnemyTempoScale);
        }
        bursting=false;
    }
}
public static partial class IndustrialMeshFactory
{
    static Mesh tankWeapon,frostWeapon;
    public static Mesh GroundWeapon(int kind)
    {
        if(kind==39)
        {
            if(tankWeapon!=null)return tankWeapon;var b=new Builder();BuildTankWeapon(b);return tankWeapon=b.Finish("Rotating tank turret and recoil barrel");
        }
        if(frostWeapon!=null)return frostWeapon;var frost=new Builder();BuildFrostWeapon(frost);return frostWeapon=frost.Finish("Frost auger twin mortar rack");
    }
    static void BuildMeadowTank(Builder b)
    {
        foreach(int s in new[]{-1,1})
        {
            b.Prism(new[]{V(-.20f,-.82f),V(.20f,-.82f),V(.23f,-.61f),V(.23f,.61f),V(.16f,.82f),V(-.16f,.82f),V(-.23f,.61f),V(-.23f,-.61f)},.30f,11,new Vector3(s*.56f,0,0),6);
            for(int link=0;link<11;link++)b.Box(new Vector3(s*.56f,-.70f+link*.14f,-.18f),new Vector3(.37f,.055f,.065f),6,11);
        }
        BevelArmor(b,new[]{V(-.41f,-.75f),V(.41f,-.75f),V(.48f,-.52f),V(.46f,.58f),V(.32f,.78f),V(-.32f,.78f),V(-.46f,.58f),V(-.48f,-.52f)},.32f,2,-.08f);
        b.Box(new Vector3(0,.47f,-.28f),new Vector3(.60f,.38f,.08f),11,2);
        for(int vent=0;vent<4;vent++)b.Box(new Vector3(0,.33f+vent*.09f,-.34f),new Vector3(.49f,.033f,.02f),6,11);
        foreach(int s in new[]{-1,1})b.Box(new Vector3(s*.33f,-.64f,-.24f),new Vector3(.13f,.12f,.07f),10,11);
    }
    static void BuildTankWeapon(Builder b)
    {
        b.Drum(Vector3.zero,.36f,.16f,11,12,6);
        BevelArmor(b,new[]{V(-.31f,-.28f),V(.31f,-.28f),V(.37f,-.05f),V(.28f,.34f),V(-.28f,.34f),V(-.37f,-.05f)},.24f,2,-.15f);
        b.Box(new Vector3(0,-.59f,-.23f),new Vector3(.16f,.73f,.16f),6,11);
        b.Box(new Vector3(0,-.95f,-.23f),new Vector3(.23f,.16f,.21f),11,6);
        b.Drum(new Vector3(-.12f,.12f,-.31f),.13f,.05f,11,8,6);
        b.Box(new Vector3(.20f,.08f,-.31f),new Vector3(.06f,.23f,.025f),10,11);
    }
}
