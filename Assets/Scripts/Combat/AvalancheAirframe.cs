using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildAvalancheAirframe(Builder b)
    {

        Armor(b,new[]{V(-1.22f,.58f),V(-1.22f,-.34f),V(-.6f,-.74f),V(.6f,-.74f),V(1.22f,-.34f),V(1.22f,.58f)},.37f,1);
        foreach(int s in new[]{-1,1})
        {
            b.Drum(new Vector3(s*.74f,-.27f,-.25f),.38f,.26f,4,12,6);
            b.Drum(new Vector3(s*.74f,-.27f,-.43f),.27f,.14f,11,12);b.Drum(new Vector3(s*.74f,-.27f,-.53f),.18f,.06f,10,12);
            Motor(b,s*.88f,.54f,.38f,.64f,1);
        }
        Armor(b,new[]{V(-.3f,.65f),V(.3f,.65f),V(.25f,-.7f),V(0,-.94f),V(-.25f,-.7f)},.26f,1,-.23f);Glass(b,0,-.4f,.36f,.35f,-.45f);
    }
}
