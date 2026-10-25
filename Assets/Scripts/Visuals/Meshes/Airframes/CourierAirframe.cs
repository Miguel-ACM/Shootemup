using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildCourierAirframe(Builder b)
    {

        BevelArmor(b, new[] { V(0, -1.29f), V(.18f, -.2f), V(.2f, .68f), V(-.2f, .68f), V(-.18f, -.2f) }, .26f, 12);
        // A single propulsion spine, two paper-thin stabilizers, and an inset dark visor.
        foreach (int s in new[] { -1, 1 })
            Armor(b, new[] { V(s * .15f, .13f), V(s * .7f, .59f), V(s * .53f, .64f), V(s * .17f, .38f) }, .075f, 6, -.04f);
        Motor(b, 0, .58f, .25f, .56f, 11);
        b.Box(new Vector3(0, -.03f, -.21f), new Vector3(.145f, .32f, .035f), 11, 11);
        b.Box(new Vector3(0, -.13f, -.24f), new Vector3(.09f, .11f, .025f), 9, 11);
        Armor(b, new[] { V(-.055f, -.4f), V(.055f, -.4f), V(0, -1.01f) }, .04f, 6, -.16f);
    }
}
