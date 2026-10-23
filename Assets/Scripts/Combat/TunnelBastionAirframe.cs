using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildTunnelBastionAirframe(Builder b)
    {
        Armor(b,new[]{V(-1.34f,-.6f),V(-1.34f,.55f),V(-.85f,1.1f),V(.85f,1.1f),V(1.34f,.55f),V(1.34f,-.6f),V(.7f,-.9f),V(-.7f,-.9f)},.3f,11);
        foreach(int s in new[]{-1,1})
        {
            Armor(b,new[]{V(s*.75f,.88f),V(s*1.25f,.5f),V(s*1.25f,-.55f),V(s*.72f,-.66f)},.35f,0,-.15f);
            Motor(b,s*.93f,.56f,.26f,.6f,0);
            for(int i=0;i<3;i++)b.Box(new Vector3(s*1.3f,.3f-i*.3f,-.2f),new Vector3(.28f,.13f,.18f),6);
        }
        b.Drum(new Vector3(0,0,-.26f),.71f,.3f,6,12,11);
        b.Drum(new Vector3(0,0,-.46f),.57f,.22f,0,8,6);
        for(int i=-1;i<=1;i++)Barrel(b,i*.31f,-.72f,1.1f,.2f,-.48f);
        b.Box(new Vector3(0,.14f,-.63f),new Vector3(.75f,.13f,.06f),3);
    }
}
