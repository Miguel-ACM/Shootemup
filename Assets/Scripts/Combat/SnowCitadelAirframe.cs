using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildSnowCitadelAirframe(Builder b)
    {

        // Lenticular armored saucer: a continuous pressure hull and eight broad armor petals.
        b.Drum(Vector3.zero,1.34f,.26f,11,32,6);
        b.Drum(new Vector3(0,0,-.19f),1.27f,.2f,4,32,6);
        for(int i=0;i<8;i++)ArcPlate(b,.58f,1.22f,i*45+2,i*45+43,-.35f,.19f,1);
        b.Drum(new Vector3(0,0,-.39f),.65f,.2f,6,16);
        b.Drum(new Vector3(0,0,-.52f),.56f,.15f,1,16);
        Glass(b,0,-.23f,.82f,.28f,-.63f);
        b.Box(new Vector3(0,.18f,-.63f),new Vector3(.46f,.32f,.1f),4);
        for(int slit=0;slit<4;slit++)b.Box(new Vector3((slit-1.5f)*.1f,.18f,-.7f),new Vector3(.045f,.23f,.03f),11);
        foreach(int s in new[]{-1,1})
        {
            Motor(b,s*.74f,.78f,.33f,.54f,1);
            Armor(b,new[]{V(s*.8f,-.76f),V(s*1.08f,-.65f),V(s*.95f,-1.18f),V(s*.62f,-1.22f)},.2f,4,-.18f);
            Barrel(b,s*.72f,-.95f,.45f,.2f,-.32f);
        }
        Barrel(b,0,-1.17f,.3f,.29f,-.13f);
    }
}
