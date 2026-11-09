using UnityEngine;

public static partial class IndustrialMeshFactory
{
    // A round raspberry yarn ball wound with lighter strands around several axes,
    // a loose dangling end and two knitting needles pushed through it.
    static void BallOfYarnModel(Builder b)
    {
        var centre = new Vector3(0, .02f, 0);
        b.Pearl(centre, Vector3.one * .29f, 6, 20, 12);
        Quaternion[] windings =
        {
            Quaternion.Euler(0, 0, 0), Quaternion.Euler(70, 0, 25), Quaternion.Euler(20, 75, 0),
            Quaternion.Euler(-55, 30, 60), Quaternion.Euler(35, -50, -40), Quaternion.Euler(90, 0, -70),
        };
        foreach (var winding in windings)
            b.Halo(centre, .29f, .02f, Vector3.one, winding, 9, 24);
        b.Sweep(centre + new Vector3(.22f, -.18f, -.08f), new Vector3(.38f, -.3f, -.1f), new Vector3(.22f, -.42f, -.05f), new Vector3(.4f, -.47f, 0), .022f, .022f, 6, false, 12);
        for (int side = -1; side <= 1; side += 2)
        {
            var from = centre + new Vector3(side * -.3f, -.26f, .05f);
            var to = centre + new Vector3(side * .26f, .4f, -.05f);
            EquipmentRod(b, from, to, .018f, 13);
            b.Ball(to, Vector3.one * .04f, 3);
        }
    }
}
