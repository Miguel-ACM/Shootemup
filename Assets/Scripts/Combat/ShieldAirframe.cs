using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildShieldAirframe(Builder b)
    {

        ArcPlate(b,.65f,1.35f,180,360,0,.38f,1);ArcPlate(b,1.05f,1.15f,190,350,-.24f,.07f,9);
        Armor(b,new[]{V(-.4f,.75f),V(.4f,.75f),V(.52f,-.32f),V(0,-.72f),V(-.52f,-.32f)},.36f,4);
        foreach(int s in new[]{-1,1}){Motor(b,s*.55f,.65f,.36f,.68f,1);b.Box(new Vector3(s*.82f,-.45f,-.25f),new Vector3(.13f,.4f,.09f),6);}
        Glass(b,0,-.12f,.45f,.45f,-.35f);
    }
}
