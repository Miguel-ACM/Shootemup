using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildRelayAirframe(Builder b)
    {

        // A fast capacitor fighter: three forward needles, swept shoulder cells, open waist.
        BevelArmor(b,new[]{V(0,-1.16f),V(.22f,-.35f),V(.19f,.56f),V(0,.73f),V(-.19f,.56f),V(-.22f,-.35f)},.34f,7);
        foreach(int s in new[]{-1,1})
        {
            BevelArmor(b,new[]{V(s*.17f,.26f),V(s*.87f,.68f),V(s*1.02f,.3f),V(s*.67f,-.37f),V(s*.45f,-.54f),V(s*.5f,.1f)},.24f,7,-.04f);
            BevelArmor(b,new[]{V(s*.54f,.34f),V(s*.79f,.42f),V(s*.68f,-.14f),V(s*.52f,-.21f)},.12f,6,-.22f);
            b.Box(new Vector3(s*.63f,.08f,-.32f),new Vector3(.08f,.34f,.035f),9,11);
            Motor(b,s*.42f,.55f,.24f,.58f,7);
            Barrel(b,s*.46f,-.65f,.6f,.075f,-.1f);
        }
        Glass(b,0,.09f,.24f,.42f,-.27f);
        Barrel(b,0,-.91f,.56f,.085f,-.13f);
    }
}
