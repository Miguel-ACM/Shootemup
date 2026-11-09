using UnityEngine;

public static partial class IndustrialMeshFactory
{
    // A squat octagonal inkwell with a paper label, a white quill and a spilled pool at its foot.
    static void InkPotModel(Builder b)
    {
        b.Can(new Vector3(.06f, -.34f, 0), .34f, .015f, 11, 11);
        b.Can(new Vector3(0, -.2f, 0), .25f, .26f, 6, 11);
        b.Can(new Vector3(0, -.03f, 0), .18f, .08f, 6, 11);
        b.Can(new Vector3(0, .05f, 0), .1f, .1f, 9, 11);
        b.Can(new Vector3(0, .11f, 0), .125f, .035f, 13, 11);
        b.Box(new Vector3(0, -.2f, -.235f), new Vector3(.3f, .13f, .02f), 1, 1);
        b.Box(new Vector3(0, -.2f, -.248f), new Vector3(.18f, .025f, .01f), 11, 11);
        b.Ball(new Vector3(.2f, -.08f, -.12f), new Vector3(.035f, .06f, .035f), 11);
        Feather(b, new Vector3(0, .05f, 0), new Vector3(.27f, .5f, -.04f), .075f, .05f);
    }
}
