using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static void LuckyPennyModel(Builder b)
    {
        b.Drum(Vector3.zero, .32f, .095f, 6, 16, 11);
        Ring(b, new Vector3(0, 0, -.055f), .26f, .28f, .015f, 9, 16);
        b.Prism(new[] { new Vector2(-.09f, -.15f), new Vector2(.09f, -.15f), new Vector2(.12f, .03f), new Vector2(.07f, .14f), new Vector2(0, .19f), new Vector2(-.09f, .13f), new Vector2(-.12f, .02f) }, .018f, 9, new Vector3(0, 0, -.065f));
        b.Drum(new Vector3(.11f, .37f, 0), .055f, .07f, 9, 8);
        Ring(b, new Vector3(.11f, .37f, -.045f), .025f, .05f, .025f, 11, 8);
    }
}
