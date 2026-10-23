using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildBeamDroneAirframe(Builder b)
    {

        // A lens suspended inside an open crescent. The central aperture is the weapon.
        BevelArmor(b,new[]{V(-.28f,.72f),V(.28f,.72f),V(.48f,.22f),V(.22f,-.19f),V(-.22f,-.19f),V(-.48f,.22f)},.38f,7);
        foreach(int s in new[]{-1,1})
        {
            BevelArmor(b,new[]{V(s*.33f,.38f),V(s*.94f,.02f),V(s*.82f,-.59f),V(s*.44f,-.93f),V(s*.52f,-.43f),V(s*.56f,-.09f)},.26f,7,-.03f);
            BevelArmor(b,new[]{V(s*.67f,.07f),V(s*.85f,-.02f),V(s*.71f,-.47f),V(s*.58f,-.56f)},.1f,6,-.2f);
            b.Box(new Vector3(s*.68f,-.23f,-.29f),new Vector3(.055f,.23f,.035f),9,11);
            Motor(b,s*.3f,.68f,.21f,.3f,7);
        }
        Ring(b,new Vector3(0,-.28f,-.2f),.2f,.36f,.16f,6,12);
        b.Ball(new Vector3(0,-.28f,-.24f),new Vector3(.19f,.22f,.12f),9);
        b.Box(new Vector3(0,.28f,-.29f),new Vector3(.14f,.22f,.055f),11);
    }
}
