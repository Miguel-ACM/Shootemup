using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static Mesh WindUpCannonModel()
    {
        var b = new Builder();
        b.Box(new Vector3(0, -.19f, 0), new Vector3(.46f, .49f, .32f), 11);
        b.Box(new Vector3(0, .21f, 0), new Vector3(.29f, .65f, .27f), 6);
        for (int i = 0; i < 4; i++)
            b.Box(new Vector3(0, .05f + i * .12f, -.02f), new Vector3(.39f, .055f, .33f), 13);
        b.Box(new Vector3(0, .58f, 0), new Vector3(.4f, .18f, .36f), 11);
        b.Box(new Vector3(0, .68f, 0), new Vector3(.22f, .025f, .17f), 9);
        b.Drum(new Vector3(-.32f, -.17f, 0), .17f, .08f, 13, 8);
        b.Box(new Vector3(-.44f, -.18f, -.04f), new Vector3(.35f, .07f, .08f), 13);
        b.Drum(new Vector3(-.6f, -.18f, -.09f), .08f, .13f, 11, 8);
        b.Box(new Vector3(0, -.44f, 0), new Vector3(.22f, .18f, .24f), 12);
        return b.Finish("Wind-up cannon equipment");
    }
}
