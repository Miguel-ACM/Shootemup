using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildSnowSiegeAirframe(Builder b)
    {

        Armor(b,new[]{V(-.98f,.55f),V(-.7f,-.62f),V(.7f,-.62f),V(.98f,.55f),V(.4f,.78f),V(-.4f,.78f)},.42f,1);
        b.Box(new Vector3(0,.15f,-.3f),new Vector3(1.2f,.47f,.25f),4);
        foreach(int s in new[]{-1,1})
        {
            Motor(b,s*.76f,.58f,.3f,.72f,1);Barrel(b,s*.38f,-.65f,1.65f,.24f,-.26f);
            for(int j=0;j<5;j++)b.Box(new Vector3(s*.38f,-.22f-j*.2f,-.42f),new Vector3(.3f,.07f,.08f),6);
        }
        Glass(b,0,.13f,.42f,.3f,-.5f);b.Box(new Vector3(0,-.5f,-.3f),new Vector3(.24f,.4f,.13f),1);
    }
}
