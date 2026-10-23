using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildCarrierAirframe(Builder b)
    {

        Armor(b,new[]{V(-.42f,1.08f),V(.42f,1.08f),V(.5f,-.9f),V(0,-1.2f),V(-.5f,-.9f)},.5f,0);
        foreach(int s in new[]{-1,1})
        {
            b.Box(new Vector3(s*.72f,.05f,0),new Vector3(.53f,1.85f,.4f),6);
            b.Box(new Vector3(s*.72f,.05f,-.26f),new Vector3(.45f,1.55f,.11f),11);
            for(int i=0;i<5;i++)b.Box(new Vector3(s*.72f,-.55f+i*.3f,-.34f),new Vector3(.38f,.18f,.1f),0);
            Motor(b,s*.87f,.8f,.3f,.76f);Barrel(b,s*.65f,-1.02f,.4f,.13f);
        }
        b.Box(new Vector3(0,.4f,-.36f),new Vector3(.47f,.85f,.19f),0);Glass(b,0,-.66f,.45f,.32f,-.36f);
        b.Box(new Vector3(0,.5f,-.49f),new Vector3(.1f,.7f,.04f),10);
    }
}
