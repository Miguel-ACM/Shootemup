using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static void ThunderheadModel(Builder b)
    {
        b.Box(new Vector3(0, -.32f, 0), new Vector3(.52f, .13f, .32f), 11, 6);
        b.Box(new Vector3(0, -.23f, 0), new Vector3(.40f, .07f, .26f), 10, 11);
        EquipmentRod(b, new Vector3(0, -.21f, 0), new Vector3(0, .25f, 0), .11f, 7);
        Vector3 P(int i) { float a = i * Mathf.PI / 3; return new Vector3(Mathf.Cos(a) * .16f, -.19f + i * .016f, Mathf.Sin(a) * .16f); }
        for (int i = 0; i < 27; i++) EquipmentRod(b, P(i), P(i + 1), .023f, 10);
        EquipmentRod(b, new Vector3(0, .24f, 0), new Vector3(0, .30f, 0), .04f, 6);
        b.Drum(new Vector3(0, .33f, 0), .18f, .12f, 9, 10, 6);
        foreach (int side in new[] { -1, 1 })
            b.Prism(new[] { new Vector2(side * .18f, .28f), new Vector2(side * .30f, .36f), new Vector2(side * .24f, .37f), new Vector2(side * .33f, .48f), new Vector2(side * .17f, .37f), new Vector2(side * .23f, .37f) }, .05f, 9, Vector3.back * .04f, 1);
    }
}
