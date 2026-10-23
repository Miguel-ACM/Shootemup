using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildBeamDroneAirframe(Builder b)
    {

        Armor(b,new[]{V(-.25f,.66f),V(.25f,.66f),V(.17f,-.1f),V(0,-.47f),V(-.17f,-.1f)},.3f,7);
        foreach(int s in new[]{-1,1})
        {
            Armor(b,new[]{V(s*.18f,.3f),V(s*.75f,-.15f),V(s*.5f,-1.12f),V(s*.26f,-1.05f),V(s*.35f,-.25f)},.17f,7,-.06f);
            for(int i=0;i<4;i++)b.Box(new Vector3(s*.42f,-.37f-i*.17f,-.2f),new Vector3(.18f,.06f,.07f),9);
            Motor(b,s*.3f,.57f,.21f,.52f,7);
        }
        b.Drum(new Vector3(0,-.05f,-.23f),.18f,.13f,6,8);b.Drum(new Vector3(0,-.05f,-.32f),.1f,.06f,9,8);
    }
}
