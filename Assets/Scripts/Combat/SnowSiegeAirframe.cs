using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildSnowSiegeAirframe(Builder b)
    {

        // A low recoil carriage with two broad cannons and a visible gap between them.
        BevelArmor(b,new[]{V(-.93f,.52f),V(-.7f,-.42f),V(.7f,-.42f),V(.93f,.52f),V(.42f,.77f),V(-.42f,.77f)},.43f,4);
        BevelArmor(b,new[]{V(-.39f,.52f),V(.39f,.52f),V(.3f,-.55f),V(0,-.79f),V(-.3f,-.55f)},.26f,1,-.22f);
        foreach(int s in new[]{-1,1})
        {
            Motor(b,s*.76f,.58f,.3f,.72f,1);
            BevelArmor(b,new[]{V(s*.4f,.22f),V(s*.81f,.17f),V(s*.82f,-.64f),V(s*.66f,-1.09f),V(s*.41f,-.93f)},.4f,1,-.23f);
            b.Box(new Vector3(s*.59f,-.32f,-.47f),new Vector3(.17f,.5f,.035f),11,11);
            Barrel(b,s*.59f,-.91f,.52f,.2f,-.3f);
            b.Box(new Vector3(s*.59f,-.18f,-.5f),new Vector3(.2f,.09f,.035f),4,11);
        }
        Glass(b,0,-.22f,.39f,.31f,-.44f);
        Inlet(b,0,.29f,.43f,.3f,-.4f,1);
    }
}
