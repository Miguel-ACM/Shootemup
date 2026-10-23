using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildBomberAirframe(Builder b)
    {

        Armor(b,new[]{V(-1.65f,.22f),V(-1.1f,-.35f),V(-.38f,-.7f),V(0,-.9f),V(.38f,-.7f),V(1.1f,-.35f),V(1.65f,.22f),V(.5f,.4f),V(0,.68f),V(-.5f,.4f)},.32f,2);
        Armor(b,new[]{V(-.48f,.5f),V(.48f,.5f),V(.35f,-.4f),V(0,-.72f),V(-.35f,-.4f)},.2f,0,-.23f);
        Glass(b,0,-.22f,.65f,.4f,-.4f);
        foreach(int s in new[]{-1,1})
        {
            Motor(b,s*.95f,.46f,.34f,.72f,2);
            b.Box(new Vector3(s*.67f,-.24f,-.25f),new Vector3(.35f,.6f,.16f),11);
            for(int i=0;i<3;i++)b.Box(new Vector3(s*.67f,-.45f+i*.2f,-.37f),new Vector3(.23f,.12f,.11f),3);
        }
    }
}
