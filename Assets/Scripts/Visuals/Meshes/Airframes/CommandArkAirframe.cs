using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildCommandArkAirframe(Builder b)
    {
        foreach(int s in new[]{-1,1})
        {
            BevelArmor(b,new[]{V(s*.68f,1.13f),V(s*1.36f,.83f),V(s*1.33f,-.75f),V(s*.99f,-1.27f),V(s*.64f,-.85f)},.48f,12);
            Motor(b,s*1.05f,.71f,.36f,.66f,12);
            Inlet(b,s*.98f,-.25f,.38f,.74f,-.31f,6);
            BevelArmor(b,new[]{V(s*.7f,.53f),V(s*1.2f,.43f),V(s*1.21f,.14f),V(s*.72f,.2f)},.12f,12,-.31f);
            Barrel(b,s*1.04f,-1.04f,.48f,.14f,-.18f);
        }
        b.Box(new Vector3(0,.65f,0),new Vector3(1.7f,.26f,.28f),6);
        b.Box(new Vector3(0,-.32f,0),new Vector3(1.7f,.22f,.22f),6);
        BevelArmor(b,new[]{V(-.38f,.83f),V(.38f,.83f),V(.47f,.13f),V(0,-.36f),V(-.47f,.13f)},.42f,12,-.25f);
        b.Box(new Vector3(0,.05f,-.5f),new Vector3(.59f,.16f,.045f),11,11);
        b.Box(new Vector3(0,.05f,-.535f),new Vector3(.45f,.06f,.025f),9,11);
        // Recessed command radar, nested inside the bridge rather than a forest of antennae.
        Ring(b,new Vector3(0,.47f,-.5f),.12f,.24f,.08f,6,12);
        b.Drum(new Vector3(0,.47f,-.55f),.115f,.035f,11,12);
        b.Box(new Vector3(0,.47f,-.58f),new Vector3(.15f,.035f,.025f),10,11);
    }
}
