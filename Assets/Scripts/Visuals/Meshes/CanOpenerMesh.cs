using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static void CanOpenerModel(Builder b)
    {
        b.Prism(new[] { new Vector2(.08f, -.35f), new Vector2(-.20f, -.31f), new Vector2(-.20f, .13f), new Vector2(-.04f, .19f), new Vector2(.12f, .12f) }, .25f, 3, Vector3.zero, 11);
        b.Prism(new[] { new Vector2(.07f, -.25f), new Vector2(.07f, -.05f), new Vector2(.39f, -.11f), new Vector2(.39f, -.30f) }, .20f, 11, Vector3.zero, 6);
        b.Box(new Vector3(.39f, -.20f, 0), new Vector3(.10f, .28f, .27f), 6, 11);
        b.Box(new Vector3(.13f, -.10f, -.13f), new Vector3(.09f, .08f, .04f), 5, 11);
        // Recessed motor vents and a high-contrast chuck make the tool readable at pickup scale.
        b.Box(new Vector3(-.055f, -.16f, -.135f), new Vector3(.21f, .22f, .03f), 11, 6);
        foreach (float y in new[] { -.22f, -.16f, -.10f })
            b.Box(new Vector3(-.055f, y, -.158f), new Vector3(.15f, .022f, .02f), 6, 11);
        EquipmentRod(b, new Vector3(-.07f, .14f, 0), new Vector3(-.07f, .28f, 0), .125f, 11);
        EquipmentRod(b, new Vector3(-.07f, .23f, 0), new Vector3(-.07f, .29f, 0), .13f, 6);
        EquipmentRod(b, new Vector3(-.07f, .29f, 0), new Vector3(-.07f, .64f, 0), .048f, 6);
        Vector3 P(int i) { float a = i * Mathf.PI / 3; return new Vector3(-.07f - Mathf.Cos(a) * .079f, .29f + i * .027f, Mathf.Sin(a) * .079f); }
        for (int i = 0; i < 13; i++) EquipmentRod(b, P(i), P(i + 1), .023f, 1);
        b.Prism(new[] { new Vector2(-.015f, .63f), new Vector2(-.07f, .73f), new Vector2(-.125f, .63f) }, .09f, 1, Vector3.zero, 6);
    }
}
