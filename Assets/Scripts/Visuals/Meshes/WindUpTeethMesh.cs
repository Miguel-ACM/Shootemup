using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static void WindUpTeethModel(Builder b)
    {
        // Two separate coral gum arches, with white teeth facing the open gap.
        b.Prism(new[] { new Vector2(-.32f, .10f), new Vector2(-.28f, .20f), new Vector2(-.17f, .25f), new Vector2(.17f, .25f), new Vector2(.28f, .20f), new Vector2(.32f, .10f) }, .30f, 5, Vector3.zero, 5);
        b.Prism(new[] { new Vector2(-.32f, -.10f), new Vector2(-.27f, -.20f), new Vector2(-.15f, -.25f), new Vector2(.15f, -.25f), new Vector2(.27f, -.20f), new Vector2(.32f, -.10f) }, .30f, 5, Vector3.zero, 5);
        for (int i = 0; i < 6; i++)
        {
            float x = -.23f + i * .092f;
            b.Box(new Vector3(x, .085f, -.16f), new Vector3(.078f, .09f, .08f), 1, 1);
            b.Box(new Vector3(x, -.085f, -.16f), new Vector3(.078f, .09f, .08f), 1, 1);
        }
        // Backward-swept end teeth make the arches dimensional from the side.
        foreach (int side in new[] { -1, 1 })
            foreach (int jaw in new[] { -1, 1 })
                b.Box(new Vector3(side * .265f, jaw * .085f, .025f), new Vector3(.07f, .09f, .15f), 1, 1);
        EquipmentRod(b, new Vector3(.27f, .03f, .02f), new Vector3(.42f, .03f, .02f), .025f, 9);
        Ring(b, new Vector3(.43f, .10f, .02f), .045f, .07f, .04f, 9, 8);
        Ring(b, new Vector3(.43f, -.04f, .02f), .045f, .07f, .04f, 9, 8);
    }
}
