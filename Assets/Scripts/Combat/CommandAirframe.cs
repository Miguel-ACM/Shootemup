using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildCommandAirframe(Builder b)
    {

        Armor(b,new[]{V(0,-1.05f),V(1.45f,-.05f),V(1.32f,.6f),V(.58f,.3f),V(0,.63f),V(-.58f,.3f),V(-1.32f,.6f),V(-1.45f,-.05f)},.35f,12);
        Armor(b,new[]{V(0,-.72f),V(.55f,-.12f),V(.38f,.48f),V(-.38f,.48f),V(-.55f,-.12f)},.24f,6,-.28f);
        Glass(b,0,-.18f,.75f,.32f,-.47f);
        foreach(int s in new[]{-1,1}){Motor(b,s*1.02f,.48f,.4f,.66f,12);Barrel(b,s*.95f,-.38f,.55f,.16f);b.Box(new Vector3(s*.62f,.15f,-.39f),new Vector3(.09f,.6f,.1f),10);}
        b.Box(new Vector3(0,.38f,-.49f),new Vector3(.12f,.45f,.15f),11);
    }
}
