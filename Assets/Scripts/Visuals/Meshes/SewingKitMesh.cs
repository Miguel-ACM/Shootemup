using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static void SewingKitModel(Builder b)
    {
        b.Box(new Vector3(0, -.20f, 0), new Vector3(.66f, .20f, .38f), 6, 11);
        b.Box(new Vector3(0, .11f, .17f), new Vector3(.66f, .42f, .06f), 6, 11);
        b.Box(new Vector3(0, .11f, .13f), new Vector3(.56f, .33f, .025f), 9, 6);
        b.Ball(new Vector3(-.19f, -.055f, -.10f), new Vector3(.11f, .10f, .09f), 3);
        EquipmentRod(b, new Vector3(-.20f, -.04f, -.18f), new Vector3(-.13f, .14f, -.18f), .01f, 1);
        EquipmentRod(b, new Vector3(.02f, -.08f, -.10f), new Vector3(.02f, .08f, -.10f), .065f, 3);
        EquipmentRod(b, new Vector3(.22f, -.10f, -.12f), new Vector3(.28f, .12f, -.12f), .012f, 1);
        EquipmentRod(b, new Vector3(.14f, -.10f, -.15f), new Vector3(.20f, .12f, -.15f), .012f, 1);
    }
}
