using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static bool BackfireItem(Builder b, string kind)
    {
        if (kind != "BackdraftItem")
            return false;
        // Twin fuel bottles and a broad downward-facing exhaust bell.
        foreach (int side in new[] { -1, 1 })
        {
            b.Can(new Vector3(side * .25f, .16f, .04f), .15f, .57f, 3, 6);
            b.Box(new Vector3(side * .17f, -.15f, 0), new Vector3(.1f, .26f, .12f), 11);
        }
        b.Prism(new[] { new Vector2(-.12f, .06f), new Vector2(.12f, .06f), new Vector2(.37f, -.45f), new Vector2(-.37f, -.45f) }, .25f, 6, Vector3.back * .06f, 11);
        b.Box(new Vector3(0, -.45f, -.07f), new Vector3(.69f, .08f, .3f), 3);
        b.Box(new Vector3(0, -.5f, -.08f), new Vector3(.47f, .04f, .16f), 1);
        return true;
    }
}
