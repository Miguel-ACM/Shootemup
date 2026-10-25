using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildScoutAirframe(Builder b)
    {

        // One engine and a gull wing keep the light interceptor legible inside a long train.
        Armor(b, new[] { V(-1.4f, .47f), V(-.46f, -.09f), V(-.18f, -.52f), V(0, -.94f), V(.18f, -.52f), V(.46f, -.09f), V(1.4f, .47f), V(.64f, .37f), V(.2f, .14f), V(-.2f, .14f), V(-.64f, .37f) }, .13f, 0);
        BevelArmor(b, new[] { V(-.19f, .47f), V(.19f, .47f), V(.22f, -.18f), V(0, -1.02f), V(-.22f, -.18f) }, .3f, 1, -.07f);
        Motor(b, 0, .46f, .3f, .62f, 0);
        Glass(b, 0, -.27f, .25f, .4f, -.3f);
        foreach (int s in new[] { -1, 1 })
        {
            BevelArmor(b, new[] { V(s * .38f, -.02f), V(s * .97f, .28f), V(s * .68f, .18f), V(s * .34f, -.19f) }, .07f, 6, -.12f);
            Barrel(b, s * .66f, -.02f, .28f, .065f, -.08f);
        }
    }
}
