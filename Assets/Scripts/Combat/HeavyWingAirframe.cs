using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildHeavyWingAirframe(Builder b)
    {
        BevelArmor(b,new[]{V(-1.52f,.72f),V(-.35f,.28f),V(.35f,.28f),V(1.52f,.72f),V(1.22f,-.4f),V(.48f,-.82f),V(-.48f,-.82f),V(-1.22f,-.4f)},.34f,2);
        BevelArmor(b,new[]{V(-.3f,.91f),V(.3f,.91f),V(.37f,-.51f),V(0,-1.2f),V(-.37f,-.51f)},.46f,0,-.1f);
        Glass(b,0,-.49f,.4f,.43f,-.41f);
        foreach(int s in new[]{-1,1})
        {
            Motor(b,s*.85f,.3f,.38f,.74f,0);
            Barrel(b,s*.68f,-.55f,1.2f,.22f,-.3f);
            BevelArmor(b,new[]{V(s*.43f,-.1f),V(s*.94f,.05f),V(s*.9f,-.68f),V(s*.46f,-.8f)},.25f,6,-.3f);
            b.Box(new Vector3(s*.68f,-.33f,-.49f),new Vector3(.19f,.37f,.03f),11,11);
            b.Box(new Vector3(s*.68f,-.17f,-.52f),new Vector3(.21f,.065f,.025f),3,11);
            Inlet(b,s*1.15f,.2f,.24f,.35f,-.23f,2);
        }
        Inlet(b,0,.26f,.35f,.43f,-.39f,0);
    }
}
