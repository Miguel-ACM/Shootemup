using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildCommandAirframe(Builder b)
    {

        BevelArmor(b,new[]{V(-.47f,.56f),V(.47f,.56f),V(.67f,-.22f),V(.28f,-.86f),V(-.28f,-.86f),V(-.67f,-.22f)},.57f,6);
        foreach(int s in new[]{-1,1})
        {
            BevelArmor(b,new[]{V(s*.43f,.28f),V(s*1.29f,.65f),V(s*1.48f,.05f),V(s*1.16f,-.61f),V(s*.79f,-.79f),V(s*.65f,-.2f)},.36f,12,-.05f);
            Motor(b,s*1.02f,.48f,.4f,.66f,12);
            BevelArmor(b,new[]{V(s*.81f,.04f),V(s*1.17f,.16f),V(s*1.13f,-.44f),V(s*.84f,-.54f)},.2f,11,-.31f);
            Barrel(b,s*.98f,-.52f,.62f,.18f,-.25f);
            b.Box(new Vector3(s*.99f,-.12f,-.45f),new Vector3(.16f,.08f,.035f),10,11);
        }
        BevelArmor(b,new[]{V(-.34f,.26f),V(.34f,.26f),V(.43f,-.21f),V(.25f,-.54f),V(-.25f,-.54f),V(-.43f,-.21f)},.22f,12,-.36f);
        b.Box(new Vector3(0,-.21f,-.51f),new Vector3(.63f,.19f,.05f),11,11);
        b.Box(new Vector3(0,-.21f,-.55f),new Vector3(.48f,.075f,.025f),9,11);
        Inlet(b,0,.48f,.45f,.27f,-.36f,6);
    }
}
