using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildSnowCitadelAirframe(Builder b)
    {

        // A round pressure hull with a domed bridge and a recessed equatorial belt.
        b.Ball(new Vector3(0, 0, .02f), new Vector3(1.31f, 1.28f, .48f), 4);
        Ring(b, new Vector3(0, 0, -.04f), 1.18f, 1.35f, .16f, 11, 32);
        for (int i = 0; i < 6; i++)
            ArcPlate(b, .83f, 1.3f, i * 60 + 4, i * 60 + 56, -.2f, .18f, 1);
        b.Ball(new Vector3(0, .04f, -.18f), new Vector3(.94f, .9f, .42f), 15);
        // Broad overlapping facets establish the dome; only three openings break its skin.
        BevelArmor(b, new[] { V(-.38f, .65f), V(.38f, .65f), V(.48f, .09f), V(.32f, -.35f), V(-.32f, -.35f), V(-.48f, .09f) }, .24f, 4, -.5f);
        Glass(b, 0, -.34f, .83f, .31f, -.64f);
        Inlet(b, 0, .28f, .43f, .32f, -.67f, 1);
        foreach (int s in new[] { -1, 1 })
        {
            Motor(b, s * .74f, .78f, .33f, .54f, 1);
            BevelArmor(b, new[] { V(s * .58f, -.44f), V(s * 1.04f, -.54f), V(s * .92f, -1.05f), V(s * .57f, -1.18f), V(s * .45f, -.78f) }, .28f, 1, -.3f);
            Inlet(b, s * .73f, -.76f, .3f, .37f, -.47f, 4);
            Barrel(b, s * .72f, -1.05f, .31f, .19f, -.25f);
            ArcPlate(b, 1.13f, 1.17f, s < 0 ? 145 : 25, s < 0 ? 166 : 46, -.31f, .035f, 9);
        }
        BevelArmor(b, new[] { V(-.21f, -.81f), V(.21f, -.81f), V(.3f, -1.16f), V(.18f, -1.32f), V(-.18f, -1.32f), V(-.3f, -1.16f) }, .27f, 4, -.16f);
        Barrel(b, 0, -1.26f, .24f, .25f, -.18f);
    }
}
