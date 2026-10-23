using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildRelayAirframe(Builder b)
    {

        b.Box(Vector3.zero,new Vector3(1.7f,.18f,.18f),6);
        foreach(int s in new[]{-1,1})
        {b.Drum(new Vector3(s*.82f,0,0),.35f,.16f,6,12);b.Drum(new Vector3(s*.82f,0,-.13f),.28f,.12f,11,12);b.Drum(new Vector3(s*.82f,0,-.23f),.18f,.1f,9,12);Motor(b,s*.42f,.55f,.22f,.58f,7);}
        Armor(b,new[]{V(0,-.88f),V(.22f,-.3f),V(.22f,.5f),V(-.22f,.5f),V(-.22f,-.3f)},.25f,7);
        b.Box(new Vector3(0,-.7f,-.18f),new Vector3(.06f,.75f,.06f),6);Glass(b,0,.06f,.22f,.3f,-.23f);
    }
}
