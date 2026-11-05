using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static void CarbonPaperModel(Builder b)
    {
        b.Box(new Vector3(.045f, -.035f, .065f), new Vector3(.52f, .68f, .045f), 1, 6);
        b.Box(new Vector3(-.025f, .015f, .015f), new Vector3(.52f, .68f, .045f), 9, 6);
        b.Box(new Vector3(-.07f, .055f, -.04f), new Vector3(.52f, .68f, .045f), 7, 11);
        b.Box(new Vector3(-.07f, .35f, -.075f), new Vector3(.52f, .105f, .055f), 11, 6);
        foreach (float x in new[] { -.23f, .09f }) b.Drum(new Vector3(x, .35f, -.11f), .025f, .025f, 10, 6);
        b.Prism(new[] { new Vector2(.12f, -.20f), new Vector2(.19f, -.20f), new Vector2(.19f, -.27f) }, .035f, 1, new Vector3(0, 0, -.08f), 6);
        b.Box(new Vector3(-.07f, .16f, -.08f), new Vector3(.26f, .025f, .025f), 9, 6);
        b.Box(new Vector3(-.10f, .08f, -.08f), new Vector3(.20f, .025f, .025f), 9, 6);
    }
}
