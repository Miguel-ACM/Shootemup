using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildThunderNeedleAirframe(Builder b)
    {
        Armor(b,new[]{V(0,-1.95f),V(-.25f,-.4f),V(-.28f,.85f),V(0,1.34f),V(.28f,.85f),V(.25f,-.4f)},.4f,7);
        Armor(b,new[]{V(-.11f,.8f),V(.11f,.8f),V(.1f,-.9f),V(0,-1.65f),V(-.1f,-.9f)},.09f,6,-.27f);
        Glass(b,0,.15f,.3f,.67f,-.34f);
        foreach(int s in new[]{-1,1})
        {
            Armor(b,new[]{V(s*.2f,.65f),V(s*.65f,.95f),V(s*.74f,-.55f),V(s*.38f,-1.64f),V(s*.42f,-.42f)},.2f,7,-.05f);
            for(int i=0;i<7;i++)b.Box(new Vector3(s*(.53f-i*.018f),.35f-i*.22f,-.2f),new Vector3(.21f,.06f,.08f),9);
            Motor(b,s*.29f,.85f,.24f,.62f,7);
            Barrel(b,s*.43f,-1.35f,.58f,.08f,-.22f);
        }
        b.Drum(new Vector3(0,-.5f,-.32f),.16f,.13f,9,12,11);
    }
}
