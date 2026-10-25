using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildArcLancerAirframe(Builder b)
    {

        // An asymmetric rail weapon gives the sweeping lancer its own outline.
        BevelArmor(b,new[]{V(-.36f,.71f),V(.21f,.66f),V(.32f,-.21f),V(.06f,-.83f),V(-.43f,-.57f)},.42f,7);
        foreach(int s in new[]{-1,1})
        {
            Motor(b,s*.42f,.58f,.25f,.66f,7);
        }
        BevelArmor(b,new[]{V(.17f,.17f),V(.72f,.4f),V(.84f,-.52f),V(.54f,-1.48f),V(.34f,-1.18f),V(.4f,-.43f)},.3f,6,-.04f);
        BevelArmor(b,new[]{V(.43f,.14f),V(.61f,.15f),V(.65f,-.52f),V(.49f,-1.13f),V(.43f,-.96f)},.12f,11,-.26f);
        b.Box(new Vector3(.54f,-.28f,-.35f),new Vector3(.075f,.55f,.04f),9,11);
        BevelArmor(b,new[]{V(-.32f,.3f),V(-.95f,.53f),V(-.83f,-.17f),V(-.48f,-.48f)},.2f,7,-.05f);
        Inlet(b,-.6f,.03f,.25f,.31f,-.2f);
        Glass(b,-.09f,-.13f,.3f,.44f,-.35f);
        Barrel(b,.52f,-1.26f,.38f,.12f,-.2f);
    }
}
