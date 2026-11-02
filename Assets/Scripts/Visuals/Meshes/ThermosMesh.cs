using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static void ThermosModel(Builder b)
    {
        EquipmentRod(b, new Vector3(0, -.31f, 0), new Vector3(0, .25f, 0), .19f, 6);
        EquipmentRod(b, new Vector3(0, .25f, 0), new Vector3(0, .36f, 0), .155f, 11);
        EquipmentRod(b, new Vector3(0, -.30f, 0), new Vector3(0, -.26f, 0), .195f, 9);
        EquipmentRod(b, new Vector3(.17f, .19f, 0), new Vector3(.29f, .19f, 0), .035f, 11);
        EquipmentRod(b, new Vector3(.29f, .19f, 0), new Vector3(.29f, -.13f, 0), .035f, 11);
        EquipmentRod(b, new Vector3(.29f, -.13f, 0), new Vector3(.17f, -.13f, 0), .035f, 11);
        b.Box(new Vector3(0, -.02f, -.19f), new Vector3(.18f, .10f, .025f), 9);
    }
}
