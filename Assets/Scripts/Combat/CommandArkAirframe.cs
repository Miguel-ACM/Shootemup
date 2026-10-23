using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildCommandArkAirframe(Builder b)
    {
        foreach(int s in new[]{-1,1})
        {
            Armor(b,new[]{V(s*.68f,1.13f),V(s*1.36f,.83f),V(s*1.33f,-.75f),V(s*.99f,-1.27f),V(s*.64f,-.85f)},.42f,12);
            Motor(b,s*1.05f,.71f,.36f,.66f,12);
            b.Box(new Vector3(s*.98f,-.27f,-.28f),new Vector3(.36f,.8f,.14f),11);
            for(int i=0;i<5;i++)b.Box(new Vector3(s*.98f,-.55f+i*.14f,-.38f),new Vector3(.28f,.06f,.04f),6);
            Barrel(b,s*1.04f,-1.04f,.48f,.14f,-.18f);
        }
        b.Box(new Vector3(0,.65f,0),new Vector3(1.7f,.26f,.28f),6);
        b.Box(new Vector3(0,-.32f,0),new Vector3(1.7f,.22f,.22f),6);
        Armor(b,new[]{V(-.38f,.83f),V(.38f,.83f),V(.47f,.13f),V(0,-.36f),V(-.47f,.13f)},.35f,12,-.31f);
        Glass(b,0,.12f,.64f,.29f,-.56f);
        b.Box(new Vector3(0,.75f,-.55f),new Vector3(.12f,.6f,.1f),11);
        b.Box(new Vector3(0,1.02f,-.55f),new Vector3(.75f,.08f,.1f),6);
        for(int i=0;i<3;i++)b.Box(new Vector3((i-1)*.19f,.48f,-.55f),new Vector3(.11f,.13f,.04f),3);
    }
}
