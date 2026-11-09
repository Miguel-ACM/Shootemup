using UnityEngine;

public static partial class IndustrialMeshFactory
{
    // A thick ember-red ledger with chalk tally marks on its cover, a brass clasp
    // and a ribbon bookmark hanging from its pages.
    static void GrudgeModel(Builder b)
    {
        b.Box(new Vector3(.015f, 0, 0), new Vector3(.48f, .6f, .12f), 15, 15);
        for (int side = -1; side <= 1; side += 2)
            b.Box(new Vector3(0, 0, side * .072f), new Vector3(.53f, .65f, .025f), 6, 11);
        b.Box(new Vector3(-.26f, 0, 0), new Vector3(.05f, .65f, .17f), 6, 6);
        for (int band = -1; band <= 1; band += 2)
            b.Box(new Vector3(-.27f, band * .2f, 0), new Vector3(.06f, .04f, .18f), 13, 13);
        b.Box(new Vector3(.27f, 0, 0), new Vector3(.06f, .13f, .19f), 13, 11);
        b.Box(new Vector3(.25f, 0, -.093f), new Vector3(.08f, .07f, .015f), 13, 13);
        // Chalk tallies: four strokes struck through, then two more.
        for (int i = 0; i < 4; i++)
            b.Box(new Vector3(-.15f + i * .06f, .1f, -.088f), new Vector3(.026f, .2f, .01f), 15, 15);
        b.Prism(new[] { new Vector2(-.21f, .03f), new Vector2(-.19f, -.005f), new Vector2(.08f, .2f), new Vector2(.06f, .23f) }, .01f, 15, new Vector3(0, 0, -.093f), 15, 15);
        for (int i = 0; i < 2; i++)
            b.Box(new Vector3(-.15f + i * .06f, -.17f, -.088f), new Vector3(.026f, .2f, .01f), 15, 15);
        b.Prism(new[] { new Vector2(.05f, -.29f), new Vector2(.1f, -.29f), new Vector2(.12f, -.46f), new Vector2(.085f, -.42f), new Vector2(.05f, -.47f) }, .012f, 3, new Vector3(0, 0, -.03f), 3, 3);
    }
}
