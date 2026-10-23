using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildStormSkimmerAirframe(Builder b)
    {

        Armor(b,new[]{V(0,-1.05f),V(.9f,.2f),V(.45f,.75f),V(0,.48f),V(-.45f,.75f),V(-.9f,.2f)},.22f,7);
        Armor(b,new[]{V(0,-.8f),V(.28f,.18f),V(0,.5f),V(-.28f,.18f)},.12f,6,-.2f);
        Glass(b,0,-.14f,.23f,.4f,-.34f);
        foreach(int s in new[]{-1,1}){Motor(b,s*.42f,.43f,.22f,.6f,7);b.Box(new Vector3(s*.67f,.15f,-.18f),new Vector3(.09f,.42f,.08f),9);}
    }
}
