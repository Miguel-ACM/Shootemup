using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildSnowMortarAirframe(Builder b)
    {
        Armor(b,new[]{V(0,1.25f),V(.58f,.84f),V(1.12f,.08f),V(.88f,-.83f),V(0,-1.18f),V(-.88f,-.83f),V(-1.12f,.08f),V(-.58f,.84f)},.4f,4);
        Armor(b,new[]{V(-.32f,.73f),V(.32f,.73f),V(.53f,-.32f),V(0,-.87f),V(-.53f,-.32f)},.2f,1,-.3f);
        Glass(b,0,-.4f,.55f,.34f,-.48f);
        foreach(int s in new[]{-1,1})
        {
            Motor(b,s*.48f,.77f,.34f,.66f,1);
            b.Drum(new Vector3(s*.73f,.02f,-.25f),.35f,.35f,6,12,11);
            b.Drum(new Vector3(s*.73f,.02f,-.47f),.27f,.15f,1,12,6);
            b.Drum(new Vector3(s*.73f,.02f,-.57f),.18f,.08f,11,12);
            b.Drum(new Vector3(s*.73f,.02f,-.62f),.1f,.04f,3,12);
            for(int i=0;i<3;i++)b.Box(new Vector3(s*.7f,-.45f-i*.11f,-.3f),new Vector3(.28f,.045f,.04f),11);
        }
        b.Box(new Vector3(0,.23f,-.44f),new Vector3(.24f,.4f,.07f),6);
    }
}
