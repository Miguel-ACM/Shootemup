using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildStormReactorAirframe(Builder b)
    {
        // Three armored transformer lobes wrap an exposed, suspended reactor lens.
        Ring(b,new Vector3(0,-.12f,.04f),.52f,.81f,.23f,11,24);
        foreach(int s in new[]{-1,1})
        {
            BevelArmor(b,new[]{V(s*.47f,.67f),V(s*1.08f,.4f),V(s*1.12f,-.33f),V(s*.77f,-.93f),V(s*.52f,-.72f),V(s*.71f,-.16f)},.38f,7);
            BevelArmor(b,new[]{V(s*.77f,.25f),V(s*.95f,.2f),V(s*.91f,-.29f),V(s*.72f,-.55f)},.14f,6,-.28f);
            b.Box(new Vector3(s*.83f,-.1f,-.38f),new Vector3(.07f,.38f,.025f),9,11);
            Motor(b,s*.56f,.65f,.29f,.56f,7);
            Armor(b,new[]{V(s*.58f,-.73f),V(s*.34f,-.98f),V(s*.46f,-.5f)},.15f,6,-.08f);
        }
        BevelArmor(b,new[]{V(-.37f,.99f),V(.37f,.99f),V(.48f,.43f),V(.23f,.19f),V(-.23f,.19f),V(-.48f,.43f)},.4f,7);
        Inlet(b,0,.67f,.4f,.29f,-.3f,6);
        b.Box(new Vector3(0,.03f,-.12f),new Vector3(.21f,.72f,.2f),11);
        Ring(b,new Vector3(0,-.35f,-.23f),.24f,.4f,.18f,6,16);
        b.Ball(new Vector3(0,-.35f,-.32f),new Vector3(.23f,.23f,.12f),9);
        b.Drum(new Vector3(0,-.35f,-.45f),.075f,.025f,11,12);
    }
}
