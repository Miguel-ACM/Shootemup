using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static void FirecrackerBeltModel(Builder b)
    {
        Vector3[] points = { new Vector3(-.31f, .15f, 0), new Vector3(-.22f, -.05f, -.02f), new Vector3(-.08f, -.18f, -.04f), new Vector3(.10f, -.19f, -.04f), new Vector3(.25f, -.07f, -.02f), new Vector3(.32f, .13f, 0) };
        for (int i = 0; i < points.Length; i++)
        {
            Vector3 p = points[i];
            EquipmentRod(b, p + Vector3.down * .13f, p + Vector3.up * .13f, .055f, 5);
            EquipmentRod(b, p + Vector3.up * .12f, p + Vector3.up * .16f, .058f, 10);
            b.Prism(new[] { new Vector2(-.055f, .16f), new Vector2(.055f, .16f), new Vector2(0, .21f) }, .10f, 5, p, 11);
            EquipmentRod(b, p + Vector3.up * .16f, p + new Vector3(.035f, .21f, 0), .012f, 11);
            if (i > 0) EquipmentRod(b, points[i - 1] + Vector3.back * .065f, p + Vector3.back * .065f, .018f, 10);
        }
        EquipmentRod(b, new Vector3(-.31f, .34f, 0), new Vector3(-.21f, .42f, 0), .018f, 11);
        b.Drum(new Vector3(-.20f, .43f, -.02f), .045f, .04f, 3, 6);
    }
}
