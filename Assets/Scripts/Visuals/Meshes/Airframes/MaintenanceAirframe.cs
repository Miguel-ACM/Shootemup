using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildMaintenanceAirframe(Builder b)
    {
        // A squat service tug with a dark tool bay and two articulated grabber jaws.
        BevelArmor(b,new[]{V(-.36f,.74f),V(.36f,.74f),V(.62f,.34f),V(.52f,-.27f),V(.26f,-.47f),V(-.26f,-.47f),V(-.52f,-.27f),V(-.62f,.34f)},.4f,0);
        BevelArmor(b,new[]{V(-.26f,.39f),V(.26f,.39f),V(.31f,-.17f),V(-.31f,-.17f)},.14f,6,-.29f);
        b.Box(new Vector3(0,.1f,-.4f),new Vector3(.38f,.37f,.035f),11,11);
        b.Drum(new Vector3(0,.16f,-.435f),.1f,.035f,9,8);
        foreach(int s in new[]{-1,1})
        {
            Armor(b,new[]{V(s*.42f,.09f),V(s*.9f,-.28f),V(s*.76f,-.84f),V(s*.48f,-.93f),V(s*.54f,-.72f),V(s*.64f,-.62f),V(s*.65f,-.34f),V(s*.36f,-.12f)},.18f,6,-.02f);
            b.Drum(new Vector3(s*.52f,-.12f,-.16f),.12f,.1f,11,8);
            b.Box(new Vector3(s*.68f,-.59f,-.15f),new Vector3(.13f,.18f,.06f),10,11);
        }
        Motor(b,0,.65f,.34f,.46f,0);
    }
}
