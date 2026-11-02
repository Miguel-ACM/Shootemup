using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static void SaltShakerModel(Builder b)
    {
        b.Prism(new[] { new Vector2(-.19f, -.30f), new Vector2(.19f, -.30f), new Vector2(.21f, .10f), new Vector2(.14f, .23f), new Vector2(-.14f, .23f), new Vector2(-.21f, .10f) }, .29f, 9, Vector3.zero, 6);
        b.Box(new Vector3(0, -.16f, -.16f), new Vector3(.30f, .16f, .025f), 1, 9);
        b.Box(new Vector3(0, .27f, 0), new Vector3(.31f, .10f, .27f), 6, 11);
        for (int i = -1; i <= 1; i++)
            b.Drum(new Vector3(i * .075f, .27f, -.145f), .018f, .012f, 11, 6);
    }
}
