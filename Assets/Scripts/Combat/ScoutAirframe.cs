using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildScoutAirframe(Builder b)
    {

        Armor(b,new[]{V(-1.5f,.55f),V(-.18f,-.42f),V(0,-.95f),V(.18f,-.42f),V(1.5f,.55f),V(.55f,.34f),V(0,.12f),V(-.55f,.34f)},.16f,0);
        Armor(b,new[]{V(-.2f,.4f),V(0,.63f),V(.2f,.4f),V(.12f,-.6f),V(0,-1.0f),V(-.12f,-.6f)},.26f,1,-.12f);
        Glass(b,0,-.18f,.23f,.48f,-.29f);
        foreach(int s in new[]{-1,1}){Motor(b,s*.55f,.45f,.22f,.66f);Barrel(b,s*.95f,.13f,.38f,.08f);}
    }
}
