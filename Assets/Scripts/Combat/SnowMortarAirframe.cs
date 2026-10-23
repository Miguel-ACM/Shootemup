using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildSnowMortarAirframe(Builder b)
    {
        BevelArmor(b,new[]{V(-.3f,1.04f),V(.3f,1.04f),V(.62f,.3f),V(.48f,-.71f),V(0,-1.18f),V(-.48f,-.71f),V(-.62f,.3f)},.54f,4);
        BevelArmor(b,new[]{V(-.23f,.77f),V(.23f,.77f),V(.33f,-.39f),V(0,-.84f),V(-.33f,-.39f)},.22f,1,-.33f);
        Glass(b,0,-.48f,.4f,.4f,-.52f);
        foreach(int s in new[]{-1,1})
        {
            Motor(b,s*.48f,.77f,.34f,.66f,1);
            // Slung mortars are long capsules, set apart from the central command hull.
            b.Box(new Vector3(s*.58f,-.04f,0),new Vector3(.56f,.45f,.2f),11);
            BevelArmor(b,new[]{V(s*.68f,.57f),V(s*1.11f,.34f),V(s*1.14f,-.47f),V(s*.92f,-.94f),V(s*.67f,-.73f)},.49f,1,-.12f);
            BevelArmor(b,new[]{V(s*.76f,.28f),V(s*.98f,.17f),V(s*.98f,-.3f),V(s*.77f,-.48f)},.11f,4,-.42f);
            Ring(b,new Vector3(s*.9f,-.51f,-.42f),.13f,.22f,.09f,6,12);
            b.Drum(new Vector3(s*.9f,-.51f,-.46f),.135f,.035f,11,12);
            b.Drum(new Vector3(s*.9f,-.51f,-.485f),.064f,.025f,3,8);
        }
        Inlet(b,0,.24f,.29f,.35f,-.49f,4);
    }
}
