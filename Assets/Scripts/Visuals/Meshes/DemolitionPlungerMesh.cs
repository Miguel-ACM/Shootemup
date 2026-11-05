using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static void DemolitionPlungerModel(Builder b)
    {
        b.Box(new Vector3(0, -.14f, 0), new Vector3(.50f, .40f, .30f), 5, 11);
        foreach (float x in new[] { -.21f, .21f }) b.Box(new Vector3(x, -.14f, -.16f), new Vector3(.05f, .39f, .04f), 10, 11);
        b.Box(new Vector3(0, -.14f, -.17f), new Vector3(.30f, .14f, .035f), 1, 11);
        b.Box(new Vector3(0, -.14f, -.20f), new Vector3(.20f, .035f, .025f), 5, 11);
        b.Box(new Vector3(0, .09f, 0), new Vector3(.54f, .08f, .34f), 10, 11);
        EquipmentRod(b, new Vector3(0, .10f, 0), new Vector3(0, .39f, 0), .045f, 6);
        EquipmentRod(b, new Vector3(-.23f, .39f, 0), new Vector3(.23f, .39f, 0), .06f, 11);
        foreach (float x in new[] { -.23f, .23f }) b.Ball(new Vector3(x, .39f, 0), Vector3.one * .064f, 10);
        EquipmentRod(b, new Vector3(.25f, -.06f, .07f), new Vector3(.36f, -.04f, .07f), .025f, 11);
    }
}
