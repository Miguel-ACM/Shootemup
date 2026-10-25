using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildWallTurretAirframe(Builder b)
    {
        // Compact squared mounting shoe and an offset armored mantlet.
        BevelArmor(b, new[] { V(-.78f, .65f), V(.78f, .65f), V(.86f, .27f), V(.66f, -.52f), V(-.66f, -.52f), V(-.86f, .27f) }, .32f, 11);
        foreach (int s in new[] { -1, 1 })
        {
            BevelArmor(b, new[] { V(s * .48f, .58f), V(s * .78f, .52f), V(s * .69f, -.42f), V(s * .43f, -.31f) }, .27f, 0, -.17f);
            b.Box(new Vector3(s * .78f, .22f, -.13f), new Vector3(.23f, .19f, .18f), 6, 11);
            b.Box(new Vector3(s * .7f, -.29f, -.13f), new Vector3(.22f, .16f, .18f), 6, 11);
            Motor(b, s * .53f, .56f, .22f, .4f, 0);
        }
        BevelArmor(b, new[] { V(-.4f, .42f), V(.4f, .42f), V(.5f, -.09f), V(.28f, -.46f), V(-.28f, -.46f), V(-.5f, -.09f) }, .43f, 0, -.18f);
        b.Box(new Vector3(0, .14f, -.44f), new Vector3(.41f, .16f, .055f), 11, 11);
        b.Box(new Vector3(0, .14f, -.48f), new Vector3(.29f, .065f, .025f), 10, 11);
        foreach (int s in new[] { -1, 1 })
            Barrel(b, s * .22f, -.67f, .8f, .14f, -.26f);
    }
}
