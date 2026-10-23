using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildSnowFighterAirframe(Builder b)
    {

        // The cold-weather interceptor has a stout blue pressure cabin and white hooked wings.
        BevelArmor(b,new[]{V(-.26f,.61f),V(.26f,.61f),V(.4f,-.25f),V(.23f,-.84f),V(0,-1.02f),V(-.23f,-.84f),V(-.4f,-.25f)},.42f,4);
        foreach(int s in new[]{-1,1})
        {
            BevelArmor(b,new[]{V(s*.22f,.26f),V(s*.78f,.68f),V(s*1.25f,.48f),V(s*1.01f,-.43f),V(s*.78f,-.61f),V(s*.81f,-.07f),V(s*.35f,-.3f)},.2f,1,-.04f);
            Motor(b,s*.55f,.48f,.3f,.64f,1);
            Armor(b,new[]{V(s*.91f,.28f),V(s*1.08f,.28f),V(s*.96f,-.18f),V(s*.87f,-.26f)},.035f,4,-.18f);
            Barrel(b,s*.89f,-.35f,.44f,.08f,-.1f);
        }
        Glass(b,0,-.27f,.34f,.51f,-.34f);
        BevelArmor(b,new[]{V(-.17f,.53f),V(.17f,.53f),V(.2f,.11f),V(-.2f,.11f)},.13f,1,-.27f);
    }
}
