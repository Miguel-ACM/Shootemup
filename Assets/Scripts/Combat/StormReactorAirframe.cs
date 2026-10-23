using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildStormReactorAirframe(Builder b)
    {
        // A transverse transformer bar with a narrow spine and two massive capacitor blocks.
        // Rectangular hardware and open joints distinguish it from the beam drone's lens crescent.
        BevelArmor(b,new[]{V(-1.38f,.28f),V(-1.19f,.57f),V(1.19f,.57f),V(1.38f,.28f),V(1.38f,-.29f),V(1.17f,-.5f),V(-1.17f,-.5f),V(-1.38f,-.29f)},.38f,11,.04f);
        BevelArmor(b,new[]{V(-.29f,1.12f),V(.29f,1.12f),V(.42f,.68f),V(.35f,-.91f),V(.18f,-1.15f),V(-.18f,-1.15f),V(-.35f,-.91f),V(-.42f,.68f)},.46f,7,-.04f);
        foreach(int s in new[]{-1,1})
        {
            // The motors retain their exact attachment points; the crossbar supports their roots.
            Motor(b,s*.56f,.65f,.29f,.56f,7);
            b.Box(new Vector3(s*.6f,.02f,-.12f),new Vector3(.52f,.3f,.23f),6,11);
            BevelArmor(b,new[]{V(s*.77f,.58f),V(s*1.28f,.5f),V(s*1.4f,.28f),V(s*1.4f,-.37f),V(s*1.18f,-.58f),V(s*.77f,-.49f)},.57f,7,-.16f);
            BevelArmor(b,new[]{V(s*.89f,.34f),V(s*1.19f,.31f),V(s*1.25f,.15f),V(s*1.25f,-.27f),V(s*.89f,-.32f)},.16f,6,-.47f);
            b.Box(new Vector3(s*1.07f,.02f,-.58f),new Vector3(.17f,.47f,.045f),11,11);
            b.Box(new Vector3(s*1.07f,.02f,-.61f),new Vector3(.075f,.29f,.025f),9,11);
            b.Box(new Vector3(s*.63f,-.31f,-.3f),new Vector3(.21f,.16f,.16f),11,11);
            b.Box(new Vector3(s*.63f,-.34f,-.4f),new Vector3(.1f,.05f,.025f),3,11);
        }
        Inlet(b,0,.7f,.36f,.36f,-.33f,6);
        // Four separated cheeks form an actual recess around the existing beam socket.
        b.Box(new Vector3(-.29f,-.35f,-.3f),new Vector3(.13f,.56f,.24f),6,11);
        b.Box(new Vector3(.29f,-.35f,-.3f),new Vector3(.13f,.56f,.24f),6,11);
        b.Box(new Vector3(0,-.08f,-.3f),new Vector3(.58f,.13f,.24f),6,11);
        b.Box(new Vector3(0,-.62f,-.3f),new Vector3(.58f,.13f,.24f),6,11);
        b.Box(new Vector3(0,-.35f,-.32f),new Vector3(.43f,.4f,.055f),11,11);
        b.Box(new Vector3(0,-.35f,-.38f),new Vector3(.15f,.26f,.04f),9,11);
        b.Box(new Vector3(0,-.88f,-.3f),new Vector3(.15f,.18f,.06f),11,11);
    }
}
