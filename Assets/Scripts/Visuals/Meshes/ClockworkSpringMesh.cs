using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static void ClockworkSpringModel(Builder b)
    {
        // A wound flat mainspring, with a loose tail and a central arbor.
        Vector3 previous = new Vector3(.035f, 0, -.06f);
        for (int i = 1; i <= 54; i++)
        {
            float a = i * Mathf.PI * 5f / 54f;
            float r = .035f + i * .0051f;
            Vector3 p = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, -.06f);
            EquipmentRod(b, previous, p, .018f, 9);
            previous = p;
        }
        EquipmentRod(b, previous, previous + new Vector3(.12f, .1f, 0), .018f, 9);
        b.Drum(new Vector3(0, 0, .015f), .065f, .19f, 11, 8);
    }
}
