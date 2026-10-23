using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildPursuitAirframe(Builder b)
    {

        BevelArmor(b,new[]{V(-.18f,.65f),V(.18f,.65f),V(.24f,-.26f),V(0,-1.38f),V(-.24f,-.26f)},.32f,11);
        foreach(int s in new[]{-1,1})
        {
            BevelArmor(b,new[]{V(s*.12f,-.65f),V(s*.39f,-.16f),V(s*1.13f,.6f),V(s*1.03f,.77f),V(s*.67f,.55f),V(s*.25f,.09f)},.2f,12,-.025f);
            Armor(b,new[]{V(s*.39f,-.11f),V(s*.91f,.52f),V(s*.8f,.52f),V(s*.28f,-.15f)},.025f,6,-.17f);
            Motor(b,s*.43f,.59f,.23f,.64f,12);Barrel(b,s*.65f,.1f,.55f,.08f);
        }
        Glass(b,0,-.27f,.23f,.58f,-.27f);
        BevelArmor(b,new[]{V(-.12f,.35f),V(.12f,.35f),V(.13f,.02f),V(-.13f,.02f)},.12f,12,-.23f);
    }
}
