using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static void HeatSinkModel(Builder b)
    {
        b.Box(new Vector3(0, -.22f, 0), new Vector3(.62f, .12f, .42f), 11, 6);
        for (int i = 0; i < 5; i++)
            b.Box(new Vector3(-.25f + i * .125f, .035f, 0), new Vector3(.065f, .43f, .42f), 6, 9);
        b.Box(new Vector3(0, -.28f, 0), new Vector3(.69f, .04f, .48f), 9, 11);
    }
}
