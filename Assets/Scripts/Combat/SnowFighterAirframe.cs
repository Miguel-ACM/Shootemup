using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildSnowFighterAirframe(Builder b)
    {

        Armor(b,new[]{V(0,-1.1f),V(1.3f,.65f),V(.75f,.85f),V(.3f,.45f),V(-.3f,.45f),V(-.75f,.85f),V(-1.3f,.65f)},.2f,1);
        Armor(b,new[]{V(0,-.85f),V(.42f,.35f),V(0,.65f),V(-.42f,.35f)},.2f,4,-.2f);
        Glass(b,0,-.24f,.28f,.5f,-.37f);
        foreach(int s in new[]{-1,1}){Motor(b,s*.55f,.48f,.32f,.64f,1);Barrel(b,s*.93f,.2f,.45f,.08f);b.Box(new Vector3(s*.92f,.5f,-.17f),new Vector3(.25f,.07f,.04f),4);}
    }
}
