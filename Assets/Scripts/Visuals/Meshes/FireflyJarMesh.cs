using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static void FireflyJarModel(Builder b)
    {
        // Faceted glass back, curved shoulders and base; the open front makes the
        // insects visible without transparent materials or sorting artifacts.
        const int sides = 12;
        float[] heights = { -.30f, -.25f, .14f, .23f, .27f };
        float[] radii = { .17f, .225f, .225f, .17f, .14f };
        Vector3 Point(int row, int segment)
        {
            float a = segment * Mathf.PI * 2 / sides;
            return new Vector3(Mathf.Cos(a) * radii[row], heights[row], Mathf.Sin(a) * radii[row] * .78f);
        }
        for (int row = 0; row < heights.Length - 1; row++)
            for (int i = 0; i < sides; i++)
            {
                float middle = (i + .5f) * Mathf.PI * 2 / sides;
                if (Mathf.Sin(middle) < -.49f)
                    continue; // front viewing window
                b.Face(row == 0 || row == 3 ? 6 : 11,
                    Point(row, i), Point(row + 1, i), Point(row + 1, i + 1), Point(row, i + 1));
            }
        // Glass rims carry the complete round silhouette through the open window.
        foreach (int row in new[] { 0, 1, 2, 3 })
            for (int i = 0; i < sides; i++)
                EquipmentRod(b, Point(row, i), Point(row, i + 1), .012f, 9);
        for (int i = 0; i < sides; i += 2)
            EquipmentRod(b, Point(2, i), Point(3, i), .012f, 9);
        EquipmentRod(b, new Vector3(0, .25f, 0), new Vector3(0, .34f, 0), .16f, 11);
        EquipmentRod(b, new Vector3(0, .32f, 0), new Vector3(0, .35f, 0), .17f, 6);
        foreach (var p in new[] { new Vector3(-.10f, .10f, -.04f), new Vector3(.09f, -.05f, -.07f), new Vector3(-.07f, -.18f, -.02f) })
        {
            b.Ball(p, new Vector3(.042f, .055f, .032f), 10);
            b.Ball(p + new Vector3(-.05f, .03f, .01f), new Vector3(.04f, .022f, .02f), 9);
            b.Ball(p + new Vector3(.05f, .03f, .01f), new Vector3(.04f, .022f, .02f), 9);
        }
    }
}
