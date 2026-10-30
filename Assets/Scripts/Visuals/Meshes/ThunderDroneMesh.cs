using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static Mesh thunderDrone;

    // Thunder boss laser drone, a small Tesla pylon. An eight-sided capacitor body
    // with a dark glass dome, lightning-bolt fins on both flanks, and an emitter barrel
    // wound with three coils pointing along local -Y (the beam direction).
    public static Mesh ThunderDrone()
    {
        if (thunderDrone != null)
            return thunderDrone;
        var b = new Builder();
        b.Drum(Vector3.zero, .42f, .34f, 13, 8, 6);
        b.Drum(new Vector3(0, 0, -.2f), .3f, .08f, 1, 8);
        b.Ball(new Vector3(0, .06f, -.3f), new Vector3(.26f, .26f, .16f), 9);
        for (int s = -1; s <= 1; s += 2)
        {
            b.Prism(new[] { V(s * .34f, .26f), V(s * .86f, .5f), V(s * .64f, .14f), V(s * .98f, -.02f), V(s * .56f, -.2f), V(s * .4f, -.06f) }, .1f, 6, new Vector3(0, 0, -.05f), 1);
            b.Box(new Vector3(s * .3f, .3f, -.12f), new Vector3(.08f, .2f, .08f), 10);
        }
        b.Prism(new[] { V(-.12f, .34f), V(0, .62f), V(.12f, .34f) }, .1f, 10, new Vector3(0, 0, -.12f));
        b.Box(new Vector3(0, -.58f, -.1f), new Vector3(.17f, .56f, .17f), 11);
        for (int i = 0; i < 3; i++)
            b.Drum(new Vector3(0, -.42f - i * .17f, -.1f), .15f, .09f, 10, 8);
        b.Drum(new Vector3(0, -.9f, -.1f), .12f, .1f, 9, 8);
        return thunderDrone = b.Finish("Thunder laser drone");
    }
}
