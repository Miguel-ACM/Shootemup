using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static void RibbonSpoolModel(Builder b)
    {
        EquipmentRod(b, new Vector3(0, -.24f, 0), new Vector3(0, .24f, 0), .20f, 6);
        EquipmentRod(b, new Vector3(0, -.05f, 0), new Vector3(0, .07f, 0), .208f, 3);
        EquipmentRod(b, new Vector3(0, -.29f, 0), new Vector3(0, -.23f, 0), .28f, 9);
        EquipmentRod(b, new Vector3(0, .23f, 0), new Vector3(0, .29f, 0), .28f, 9);
        EquipmentRod(b, new Vector3(.18f, -.06f, -.12f), new Vector3(.37f, -.20f, -.12f), .025f, 3);
    }
}
