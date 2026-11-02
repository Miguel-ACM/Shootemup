using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static void RollerBearingsModel(Builder b)
    {
        Ring(b, Vector3.zero, .26f, .34f, .18f, 6, 16);
        Ring(b, new Vector3(0, 0, -.015f), .11f, .17f, .20f, 11, 12);
        for (int i = 0; i < 10; i++)
        {
            float a = i * Mathf.PI * 2 / 10;
            Vector3 u = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
            EquipmentRod(b, u * .175f + Vector3.back * .115f, u * .255f + Vector3.back * .115f, .033f, 9);
        }
    }
}
