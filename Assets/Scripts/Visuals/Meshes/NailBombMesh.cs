using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static void NailBombModel(Builder b)
    {
        EquipmentRod(b, new Vector3(0, -.25f, 0), new Vector3(0, .24f, 0), .22f, 2);
        foreach (float y in new[] { -.22f, .20f })
            EquipmentRod(b, new Vector3(0, y - .035f, 0), new Vector3(0, y + .035f, 0), .235f, 11);
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4;
            Vector3 d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            Vector3 c = Vector3.up * (i % 2 == 0 ? .08f : -.08f);
            EquipmentRod(b, c + d * .18f, c + d * .36f, .014f, 6);
            EquipmentRod(b, c + d * .34f, c + d * .36f, .035f, 11);
        }
        b.Box(new Vector3(0, 0, -.23f), new Vector3(.20f, .15f, .035f), 10, 11);
        EquipmentRod(b, new Vector3(0, .25f, 0), new Vector3(.03f, .34f, 0), .035f, 11);
        EquipmentRod(b, new Vector3(.03f, .34f, 0), new Vector3(.15f, .42f, 0), .022f, 10);
        b.Drum(new Vector3(.15f, .42f, -.02f), .045f, .05f, 3, 6);
    }
}
