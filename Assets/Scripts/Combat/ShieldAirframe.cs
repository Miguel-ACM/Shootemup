using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildShieldAirframe(Builder b)
    {

        ArcPlate(b,.72f,1.32f,183,357,0,.34f,11);
        ArcPlate(b,.88f,1.3f,188,242,-.2f,.17f,1);ArcPlate(b,.88f,1.3f,247,293,-.2f,.17f,1);ArcPlate(b,.88f,1.3f,298,352,-.2f,.17f,1);
        ArcPlate(b,.99f,1.04f,194,346,-.3f,.025f,9);
        BevelArmor(b,new[]{V(-.36f,.8f),V(.36f,.8f),V(.48f,.04f),V(.24f,-.46f),V(-.24f,-.46f),V(-.48f,.04f)},.48f,4);
        foreach(int s in new[]{-1,1})
        {
            Motor(b,s*.55f,.65f,.36f,.68f,1);
            BevelArmor(b,new[]{V(s*.28f,.22f),V(s*.96f,-.04f),V(s*1.04f,-.4f),V(s*.7f,-.52f),V(s*.42f,-.19f)},.21f,1,-.08f);
        }
        Glass(b,0,-.1f,.45f,.4f,-.36f);Inlet(b,0,.47f,.37f,.25f,-.32f,1);
    }
}
