using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildCourierAirframe(Builder b)
    {

        Armor(b,new[]{V(0,-1.28f),V(.16f,-.2f),V(.63f,.6f),V(.23f,.42f),V(.2f,.72f),V(-.2f,.72f),V(-.23f,.42f),V(-.63f,.6f),V(-.16f,-.2f)},.18f,12);
        Glass(b,0,-.05f,.19f,.5f,-.18f);foreach(int s in new[]{-1,1})Motor(b,s*.2f,.58f,.16f,.56f,11);
        b.Box(new Vector3(0,-.75f,-.12f),new Vector3(.07f,.38f,.07f),6);
    }
}
