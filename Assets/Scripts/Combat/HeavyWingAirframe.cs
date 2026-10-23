using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildHeavyWingAirframe(Builder b)
    {
        Armor(b,new[]{V(-1.52f,.72f),V(-.35f,.28f),V(.35f,.28f),V(1.52f,.72f),V(1.22f,-.4f),V(.48f,-.82f),V(-.48f,-.82f),V(-1.22f,-.4f)},.31f,2);
        Armor(b,new[]{V(-.35f,.91f),V(.35f,.91f),V(.4f,-.52f),V(0,-1.2f),V(-.4f,-.52f)},.38f,0,-.1f);
        Glass(b,0,-.42f,.45f,.55f,-.37f);
        foreach(int s in new[]{-1,1})
        {
            Motor(b,s*.85f,.3f,.38f,.74f,0);
            Barrel(b,s*.68f,-.55f,1.2f,.22f,-.3f);
            Armor(b,new[]{V(s*.43f,-.1f),V(s*.94f,.05f),V(s*.9f,-.68f),V(s*.46f,-.8f)},.15f,6,-.38f);
            for(int i=0;i<3;i++)b.Box(new Vector3(s*.68f,-.17f-i*.18f,-.5f),new Vector3(.28f,.07f,.04f),3);
        }
    }
}
