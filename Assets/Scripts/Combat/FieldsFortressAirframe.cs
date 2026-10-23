using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildFieldsFortressAirframe(Builder b)
    {
        Armor(b,new[]{V(-1.7f,.5f),V(-.58f,1.1f),V(0,.8f),V(.58f,1.1f),V(1.7f,.5f),V(1.36f,-.4f),V(.7f,-.72f),V(0,-.32f),V(-.7f,-.72f),V(-1.36f,-.4f)},.34f,2);
        Armor(b,new[]{V(-.43f,.8f),V(.43f,.8f),V(.62f,-.4f),V(0,-1.13f),V(-.62f,-.4f)},.46f,0,-.18f);
        Glass(b,0,-.38f,.68f,.34f,-.52f);
        foreach(int s in new[]{-1,1})
        {
            Motor(b,s*1.04f,.35f,.43f,.74f,2);
            Armor(b,new[]{V(s*.6f,-.1f),V(s*1.47f,.1f),V(s*1.3f,-.57f),V(s*.75f,-.9f)},.18f,6,-.24f);
            for(int i=0;i<3;i++)
            {
                float x=s*(.75f+i*.22f),y=-.48f+i*.12f;
                b.Drum(new Vector3(x,y,-.42f),.1f,.14f,11,8,6);b.Drum(new Vector3(x,y,-.51f),.055f,.045f,3,8);
            }
            Barrel(b,s*1.4f,-.35f,.6f,.16f,-.13f);
        }
        for(int i=0;i<4;i++)b.Box(new Vector3(0,.12f+i*.12f,-.47f),new Vector3(.5f,.04f,.035f),11);
    }
}
