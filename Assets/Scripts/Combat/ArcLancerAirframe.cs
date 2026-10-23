using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildArcLancerAirframe(Builder b)
    {

        Armor(b,new[]{V(-.28f,.6f),V(.28f,.6f),V(.75f,-.8f),V(.43f,-1.3f),V(.16f,-.15f),V(-.16f,-.15f),V(-.43f,-1.3f),V(-.75f,-.8f)},.25f,7);
        foreach(int s in new[]{-1,1})
        {
            Motor(b,s*.42f,.58f,.25f,.66f,7);
            for(int i=0;i<5;i++)b.Box(new Vector3(s*(.29f+i*.055f),-.18f-i*.19f,-.22f),new Vector3(.23f,.07f,.1f),9);
        }
        Glass(b,0,.29f,.35f,.34f,-.24f);b.Drum(new Vector3(0,-.08f,-.2f),.14f,.16f,10,8);
    }
}
