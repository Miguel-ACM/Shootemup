using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildStormSkimmerAirframe(Builder b)
    {

        BevelArmor(b, new[] { V(0, -1.15f), V(.23f, -.22f), V(.22f, .53f), V(-.22f, .53f), V(-.23f, -.22f) }, .28f, 7);
        foreach (int s in new[] { -1, 1 })
        {
            BevelArmor(b, new[] { V(s * .17f, -.35f), V(s * .92f, -.07f), V(s * 1.1f, .33f), V(s * .67f, .22f), V(s * .39f, .66f), V(s * .26f, .32f) }, .19f, 7, -.04f);
            Armor(b, new[] { V(s * .5f, -.12f), V(s * .94f, .16f), V(s * .63f, .07f) }, .035f, 6, -.17f);
            Motor(b, s * .42f, .58f, .21f, .3f, 7);
            b.Box(new Vector3(s * .67f, .06f, -.22f), new Vector3(.13f, .06f, .035f), 9, 11);
        }
        Glass(b, 0, -.16f, .22f, .52f, -.27f);
        Inlet(b, 0, .31f, .24f, .22f, -.23f, 7);
    }
}
