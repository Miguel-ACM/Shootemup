using UnityEngine;

public static partial class IndustrialMeshFactory
{
    // A mahogany hourglass frame with three turned posts around two pale glass bulbs;
    // the lower bulb has filled with golden sand.
    static void HourglassModel(Builder b)
    {
        foreach (float y in new[] { -.4f, .4f })
        {
            b.Can(new Vector3(0, y, 0), .28f, .06f, 6, 6);
            b.Can(new Vector3(0, y - Mathf.Sign(y) * .045f, 0), .23f, .03f, 11, 11);
        }
        for (int i = 0; i < 3; i++)
        {
            float a = (90 + i * 120) * Mathf.Deg2Rad;
            var foot = new Vector3(Mathf.Cos(a) * .22f, 0, Mathf.Sin(a) * .22f);
            EquipmentRod(b, foot + Vector3.down * .37f, foot + Vector3.up * .37f, .025f, 6);
            b.Ball(foot, new Vector3(.038f, .05f, .038f), 9);
        }
        b.Pearl(new Vector3(0, .18f, 0), new Vector3(.16f, .17f, .16f), 15, 16, 10);
        b.Pearl(new Vector3(0, -.18f, 0), new Vector3(.16f, .17f, .16f), 10, 16, 10);
        b.Can(new Vector3(0, 0, 0), .045f, .08f, 15, 15);
    }
}
