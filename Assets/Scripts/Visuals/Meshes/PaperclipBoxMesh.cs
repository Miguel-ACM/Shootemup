using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static void PaperclipBoxModel(Builder b)
    {
        b.Box(new Vector3(0, -.18f, 0), new Vector3(.61f, .28f, .37f), 6, 11);
        b.Box(new Vector3(0, -.015f, .13f), new Vector3(.62f, .07f, .09f), 9);
        b.Box(new Vector3(-.28f, .02f, 0), new Vector3(.07f, .15f, .32f), 9);
        b.Box(new Vector3(.28f, .02f, 0), new Vector3(.07f, .15f, .32f), 9);
        for (int i = 0; i < 4; i++)
        {
            float x = -.19f + i * .125f, z = -.05f - i * .025f;
            EquipmentRod(b, new Vector3(x, -.07f, z), new Vector3(x, .23f, z), .017f, 9);
            EquipmentRod(b, new Vector3(x, .23f, z), new Vector3(x + .07f, .28f, z), .017f, 9);
            EquipmentRod(b, new Vector3(x + .07f, .28f, z), new Vector3(x + .095f, .03f, z), .017f, 9);
            EquipmentRod(b, new Vector3(x + .095f, .03f, z), new Vector3(x + .055f, -.02f, z), .017f, 9);
        }
    }
}
