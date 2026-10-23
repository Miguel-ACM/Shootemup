using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildAvalancheAirframe(Builder b)
    {

        BevelArmor(b,new[]{V(-1.22f,.42f),V(-1.08f,-.46f),V(-.56f,-.76f),V(.56f,-.76f),V(1.08f,-.46f),V(1.22f,.42f),V(.57f,.7f),V(-.57f,.7f)},.39f,4);
        foreach(int s in new[]{-1,1})
        {
            // Two pressure vessels read as the payload, with recessed warm shell ports.
            b.Ball(new Vector3(s*.69f,-.17f,-.2f),new Vector3(.45f,.63f,.31f),15);
            Ring(b,new Vector3(s*.69f,-.37f,-.44f),.16f,.28f,.09f,6,12);
            b.Drum(new Vector3(s*.69f,-.37f,-.48f),.16f,.03f,11,12);
            b.Drum(new Vector3(s*.69f,-.37f,-.5f),.09f,.025f,10,8);
            Armor(b,new[]{V(s*.46f,.19f),V(s*.87f,.23f),V(s*.87f,.34f),V(s*.46f,.3f)},.045f,4,-.48f);
            Motor(b,s*.88f,.54f,.38f,.64f,1);
        }
        BevelArmor(b,new[]{V(-.24f,.65f),V(.24f,.65f),V(.25f,-.67f),V(0,-.98f),V(-.25f,-.67f)},.28f,1,-.23f);
        Glass(b,0,-.44f,.31f,.36f,-.47f);Inlet(b,0,.22f,.3f,.4f,-.42f,4);
    }
}
