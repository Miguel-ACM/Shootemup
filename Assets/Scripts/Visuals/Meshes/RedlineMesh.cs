using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static void RedlineModel(Builder b)
    {
        b.Drum(Vector3.zero, .34f, .22f, 5, 12, 11);
        b.Drum(new Vector3(0, 0, -.13f), .285f, .05f, 1, 12, 6);
        for (int i = 0; i < 7; i++)
        {
            float a = (-135 + i * 45) * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0);
            EquipmentRod(b, p * .21f + Vector3.back * .17f, p * .255f + Vector3.back * .17f, .012f, i >= 5 ? 5 : 11);
        }
        EquipmentRod(b, new Vector3(0, 0, -.19f), new Vector3(.21f, -.13f, -.19f), .022f, 5);
        b.Drum(new Vector3(0, 0, -.20f), .045f, .04f, 11, 8);
        b.Box(new Vector3(0, -.40f, 0), new Vector3(.16f, .15f, .16f), 10, 11);
        b.Box(new Vector3(0, -.49f, 0), new Vector3(.25f, .055f, .19f), 6, 11);
    }
}
