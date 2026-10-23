using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildPatrolAirframe(Builder b)
    {

        Armor(b,new[]{V(-.45f,.55f),V(.45f,.55f),V(.48f,-.42f),V(.24f,-.84f),V(-.24f,-.84f),V(-.48f,-.42f)},.38f,0);
        b.Box(new Vector3(0,.13f,-.06f),new Vector3(1.7f,.28f,.22f),6);
        foreach(int s in new[]{-1,1}){Motor(b,s*.64f,.44f,.4f,.98f);Barrel(b,s*.63f,-.48f,.85f,.14f);}
        Glass(b,0,-.3f,.5f,.4f,-.3f);b.Box(new Vector3(0,.38f,-.32f),new Vector3(.42f,.28f,.12f),11);
        for(int i=0;i<3;i++)b.Box(new Vector3((i-1)*.14f,.38f,-.4f),new Vector3(.05f,.22f,.03f),10);
    }
}
