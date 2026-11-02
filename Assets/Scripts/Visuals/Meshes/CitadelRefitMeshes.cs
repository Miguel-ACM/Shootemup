using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static Mesh citadelRefit, citadelReactor;
    public static Mesh CitadelRefit()
    {
        if (citadelRefit != null) return citadelRefit;
        var b = new Builder();
        // A continuous orbital fortress: ivory pressure hull, bronze load paths,
        // swept photovoltaic wings and one deep central reactor aperture.
        foreach (int side in new[] { -1, 1 })
        {
            BevelArmor(b, new[] { V(side * .72f, -.56f), V(side * 1.58f, -.95f), V(side * 2.62f, -.70f),
                V(side * 2.9f, .42f), V(side * 1.7f, .92f), V(side * .75f, .65f) }, .48f, 15, .11f);
            BevelArmor(b, new[] { V(side * .91f, .5f), V(side * 1.52f, 1.30f), V(side * 2.68f, 1.53f),
                V(side * 3.08f, 1.26f), V(side * 2.93f, .66f), V(side * 1.68f, .67f) }, .23f, 1, .04f);
            b.Box(new Vector3(side * 1.36f, .2f, .08f), new Vector3(.74f, .23f, .35f), 13, 6);
            for (int panel = 0; panel < 3; panel++)
            {
                float x = side * (1.82f + panel * .38f);
                SolarPanel(b, new Vector3(x, .10f + panel * .10f, -.20f), .32f, .83f);
                b.Box(new Vector3(x, -.64f + panel * .04f, -.12f), new Vector3(.26f, .16f, .20f), 13, 6);
            }
            // Recessed radiator stacks have actual edges and volume.
            for (int fin = 0; fin < 4; fin++)
                b.Box(new Vector3(side * 1.64f, -.57f - fin * .12f, .15f), new Vector3(.58f, .045f, .42f), 15, 6);
            BevelArmor(b, new[] { V(side * .58f, -.81f), V(side * 1.0f, -1.12f), V(side * 1.23f, -1.58f),
                V(side * .76f, -1.34f), V(side * .41f, -.9f) }, .28f, 1, -.07f);
        }
        Ring(b, new Vector3(0, 0, .02f), .76f, 1.24f, .62f, 15, 12);
        Ring(b, new Vector3(0, 0, -.29f), .78f, .87f, .10f, 13, 12);
        for (int i = 0; i < 8; i++)
        {
            float a = (22.5f + i * 45) * Mathf.Deg2Rad;
            var p = new Vector3(Mathf.Cos(a) * 1.075f, Mathf.Sin(a) * 1.075f, -.34f);
            b.Prism(new[] { V(-.06f, -.13f), V(.06f, -.13f), V(.08f, .13f), V(-.08f, .13f) }, .08f, 13, p, 6);
        }
        BevelArmor(b, new[] { V(-.39f, 1.03f), V(-.31f, 1.59f), V(0, 1.93f), V(.31f, 1.59f), V(.39f, 1.03f) }, .46f, 1, .16f);
        b.Box(new Vector3(0, 1.43f, -.14f), new Vector3(.37f, .12f, .06f), 11, 6);
        b.Box(new Vector3(0, 1.44f, -.18f), new Vector3(.26f, .025f, .02f), 9, 11);
        BevelArmor(b, new[] { V(-.34f, -.99f), V(-.4f, -1.37f), V(-.23f, -1.72f), V(.23f, -1.72f),
            V(.4f, -1.37f), V(.34f, -.99f) }, .48f, 15, .06f);
        b.Box(new Vector3(0, -1.41f, -.23f), new Vector3(.32f, .36f, .10f), 11, 6);
        b.Box(new Vector3(0, -1.49f, -.30f), new Vector3(.21f, .11f, .08f), 9, 13);
        return citadelRefit = b.Finish("Armored orbital citadel with a central reactor aperture");
    }

    public static Mesh CitadelReactor()
    {
        if (citadelReactor != null) return citadelReactor;
        var b = new Builder();
        b.Drum(Vector3.forward * .06f, .76f, .18f, 11, 16, 6);
        Ring(b, Vector3.zero, .60f, .76f, .12f, 13, 16);
        b.Ball(new Vector3(0, 0, -.04f), new Vector3(.59f, .59f, .19f), 9);
        Ring(b, new Vector3(0, 0, -.12f), .41f, .445f, .025f, 10, 16);
        b.Prism(new[] { V(0, .19f), V(.14f, 0), V(0, -.19f), V(-.14f, 0) }, .06f, 10, Vector3.back * .23f, 13);
        return citadelReactor = b.Finish("Central gravity reactor lens");
    }
}
