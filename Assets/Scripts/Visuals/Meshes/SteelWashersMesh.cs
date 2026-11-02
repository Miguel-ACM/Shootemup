using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static void SteelWashersModel(Builder b)
    {
        for (int i = 0; i < 3; i++)
            Ring(b, new Vector3((i - 1) * .01f, (i - 1) * .025f, i * -.075f), .18f, .32f, .085f, 6, 12);
    }
}
