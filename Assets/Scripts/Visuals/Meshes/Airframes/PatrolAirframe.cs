using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildPatrolAirframe(Builder b)
    {

        BevelArmor(b,new[]{V(-.34f,.54f),V(.34f,.54f),V(.42f,-.4f),V(.22f,-.79f),V(-.22f,-.79f),V(-.42f,-.4f)},.48f,0);
        b.Box(new Vector3(0,.15f,.015f),new Vector3(1.5f,.31f,.2f),11);
        foreach(int s in new[]{-1,1})
        {
            BevelArmor(b,new[]{V(s*.47f,.67f),V(s*.82f,.61f),V(s*.85f,-.55f),V(s*.64f,-.84f),V(s*.47f,-.55f)},.34f,0,-.04f);
            Motor(b,s*.64f,.66f,.29f,.54f);
            Inlet(b,s*.65f,.06f,.26f,.39f,-.27f);
            Barrel(b,s*.64f,-.61f,.69f,.13f,-.12f);
        }
        Glass(b,0,-.36f,.43f,.31f,-.35f);
        BevelArmor(b,new[]{V(-.23f,.4f),V(.23f,.4f),V(.23f,.06f),V(-.23f,.06f)},.16f,6,-.3f);
        b.Box(new Vector3(0,.23f,-.4f),new Vector3(.25f,.065f,.03f),10,11);
    }
}
