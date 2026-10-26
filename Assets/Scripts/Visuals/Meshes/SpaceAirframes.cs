using UnityEngine;

public static class SpaceMaterials
{
    static Material fleet;
    public static Material Fleet
    {
        get
        {
            if (fleet != null)
                return fleet;
            fleet = new Material(CombatMaterials.Fleet) { name = "Orbital ivory, gold foil and blue solar cells" };
            var atlas = Object.Instantiate((Texture2D)fleet.mainTexture);
            atlas.name = "Orbital solar-cell trim atlas";
            foreach (int tile in new[] { 6, 13, 15 })
                for (int y = 0; y < 64; y++)
                    for (int x = 0; x < 64; x++)
                    {
                        var baseColor = tile == 15 ? new Color(.66f, .7f, .71f) : tile == 13 ? new Color(.48f, .36f, .18f) : new Color(.105f, .15f, .20f);
                        float grain = .92f + ((x * 7 + y * 11) % 17) * .006f;
                        if (x < 3 || y < 3 || x > 60 || y > 60)
                            grain *= .69f;
                        atlas.SetPixel((tile % 4) * 64 + x, (tile / 4) * 64 + y, baseColor * grain);
                    }
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    bool rail = x % 12 < 2 || y % 12 < 2;
                    var c = rail ? new Color(.33f, .44f, .57f) : Color.Lerp(new Color(.055f, .13f, .25f), new Color(.16f, .29f, .45f), (y % 12) / 12f);
                    atlas.SetPixel(3 * 64 + x, 64 + y, c);
                }
            atlas.Apply();
            fleet.mainTexture = atlas;
            return fleet;
        }
    }
}

public static partial class IndustrialMeshFactory
{
    static readonly Mesh[] orbitalParts = new Mesh[4];
    static void SolarPanel(Builder b, Vector3 p, float width, float height)
    {
        b.Box(p, new Vector3(width, height, .075f), 7, 13);
        b.Box(p + new Vector3(0, height * .5f, -.015f), new Vector3(width + .06f, .055f, .10f), 15, 6);
        b.Box(p - new Vector3(0, height * .5f, .015f), new Vector3(width + .06f, .055f, .10f), 15, 6);
        foreach (int s in new[] { -1, 1 })
            b.Box(p + new Vector3(s * width * .5f, 0, -.015f), new Vector3(.05f, height, .10f), 13, 6);
    }

    static void SpaceDish(Builder b, Vector3 p, float radius)
    {
        for (int i = 0; i < 10; i++)
        {
            float a = i * Mathf.PI / 5, n = (i + 1) * Mathf.PI / 5;
            b.Face(i % 3 == 0 ? 6 : 15, p + new Vector3(Mathf.Cos(n) * radius, Mathf.Sin(n) * radius, -.25f),
                p + new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, -.25f), p + Vector3.forward * .04f);
        }
        b.Box(p + Vector3.back * .2f, new Vector3(.045f, .045f, .5f), 13, 6);
        b.Ball(p + Vector3.back * .47f, Vector3.one * .11f, 9);
    }

    static void BuildOrbitalScout(Builder b)
    {
        BevelArmor(b, new[] { V(0, -.88f), V(-.26f, -.48f), V(-.32f, .31f), V(0, .72f), V(.32f, .31f), V(.26f, -.48f) }, .32f, 15);
        foreach (int s in new[] { -1, 1 })
        {
            b.Box(new Vector3(s * .58f, .12f, .03f), new Vector3(.69f, .07f, .1f), 13, 6);
            for (int cell = 0; cell < 3; cell++)
                SolarPanel(b, new Vector3(s * (.82f + cell * .14f), .48f - cell * .43f, .08f), .5f, .38f);
            b.Box(new Vector3(s * .19f, -.53f, -.15f), new Vector3(.12f, .3f, .16f), 11, 6);
            b.Box(new Vector3(s * .18f, .27f, -.23f), new Vector3(.12f, .3f, .04f), 13, 6);
        }
        BevelArmor(b, new[] { V(-.14f, .22f), V(.14f, .22f), V(.12f, -.25f), V(0, -.42f), V(-.12f, -.25f) }, .11f, 11, -.23f);
        b.Box(new Vector3(0, -.1f, -.32f), new Vector3(.11f, .2f, .04f), 9, 11);
        b.Box(new Vector3(0, .81f, .02f), new Vector3(.035f, .4f, .04f), 13, 6);
    }

    static void BuildLaserSatellite(Builder b)
    {
        foreach (int s in new[] { -1, 1 })
        {
            BevelArmor(b, new[] { V(s * .12f, -.77f), V(s * .34f, -.77f), V(s * .4f, .5f), V(s * .2f, .88f) }, .23f, 15);
            b.Box(new Vector3(s * .61f, .38f, .02f), new Vector3(.65f, .07f, .1f), 13, 6);
            SolarPanel(b, new Vector3(s * .89f, .4f, .05f), .48f, .85f);
            b.Prism(new[] { V(s * .25f, .82f), V(s * .57f, 1.1f), V(s * .44f, .24f) }, .08f, 13, Vector3.zero, 6);
        }
        SpaceDish(b, new Vector3(0, .28f, -.18f), .53f);
        b.Box(new Vector3(0, -.65f, -.1f), new Vector3(.29f, .8f, .25f), 6, 11);
        b.Drum(new Vector3(0, -.92f, -.24f), .17f, .18f, 13, 10, 11);
        b.Drum(new Vector3(0, -1.05f, -.24f), .12f, .06f, 9, 10, 11);
        for (int i = 0; i < 4; i++)
            b.Box(new Vector3(0, -.38f - i * .13f, -.26f), new Vector3(.37f, .04f, .08f), 15, 6);
    }

    static void BuildOrbitalCarrier(Builder b)
    {
        Ring(b, Vector3.zero, .29f, .65f, .34f, 15, 12);
        foreach (int s in new[] { -1, 1 })
        {
            BevelArmor(b, new[] { V(s * .37f, -.32f), V(s * .78f, -.55f), V(s * .92f, .57f), V(s * .51f, .92f) }, .23f, 15);
            b.Box(new Vector3(s * .89f, .28f, .02f), new Vector3(.7f, .1f, .13f), 13, 6);
            for (int panel = 0; panel < 2; panel++)
                SolarPanel(b, new Vector3(s * 1.08f, .47f - panel * .7f, .06f), .4f, .62f);
            b.Box(new Vector3(s * .52f, -.42f, -.24f), new Vector3(.12f, .4f, .13f), 11, 6);
            b.Box(new Vector3(s * .43f, .63f, -.2f), new Vector3(.17f, .25f, .06f), 13, 6);
        }
        b.Box(new Vector3(0, -.48f, -.04f), new Vector3(.26f, .86f, .28f), 6, 11);
        b.Drum(new Vector3(0, -.87f, -.2f), .14f, .13f, 9, 8, 13);
        b.Box(new Vector3(0, .65f, 0), new Vector3(.3f, .44f, .32f), 15, 13);
        SpaceDish(b, new Vector3(0, .92f, -.08f), .25f);
    }

    static void BuildOrbitalMoonlet(Builder b)
    {
        BevelArmor(b, new[] { V(0, -.51f), V(-.27f, -.1f), V(-.22f, .3f), V(.09f, .52f), V(.31f, .19f), V(.22f, -.2f) }, .28f, 15);
        foreach (int s in new[] { -1, 1 })
        {
            b.Box(new Vector3(s * .4f, .16f, .02f), new Vector3(.43f, .055f, .08f), 13, 6);
            b.Prism(new[] { V(s * .31f, -.1f), V(s * .82f, .37f), V(s * .55f, .69f), V(s * .37f, .4f) }, .055f, 7, Vector3.forward * .05f, 13);
        }
        b.Box(new Vector3(0, -.22f, -.17f), new Vector3(.13f, .25f, .13f), 11, 6);
        b.Drum(new Vector3(0, .13f, -.23f), .12f, .09f, 9, 8, 13);
    }

    static void BuildSpaceCitadel(Builder b)
    {
        // An open truss station, with sockets for genuinely separate destructible modules.
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4, n = (i + 1) * Mathf.PI / 4;
            Vector3 u = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0), v = new Vector3(Mathf.Cos(n), Mathf.Sin(n), 0);
            b.Face(15, u * .67f + Vector3.back * .22f, v * .67f + Vector3.back * .22f, v * .94f + Vector3.back * .22f, u * .94f + Vector3.back * .22f);
            b.Face(6, u * .94f + Vector3.back * .22f, v * .94f + Vector3.back * .22f, v * .94f + Vector3.forward * .2f, u * .94f + Vector3.forward * .2f);
        }
        foreach (int s in new[] { -1, 1 })
        {
            b.Box(new Vector3(s * 1.25f, .24f, .12f), new Vector3(1.3f, .18f, .25f), 13, 6);
            b.Box(new Vector3(s * 1.17f, -.70f, .13f), new Vector3(.18f, 1.6f, .22f), 15, 6);
            b.Box(new Vector3(s * 1.78f, .82f, .15f), new Vector3(.12f, 1.0f, .13f), 13, 6);
            SolarPanel(b, new Vector3(s * 1.42f, 1.22f, .15f), .7f, .48f);
        }
        b.Box(new Vector3(0, -1.16f, -.05f), new Vector3(.55f, .65f, .5f), 15, 6);
        b.Drum(new Vector3(0, -1.47f, -.28f), .22f, .15f, 9, 10, 13);
        SpaceDish(b, new Vector3(0, 1.1f, .06f), .38f);
    }

    public static Mesh SpaceModule(int kind)
    {
        if (orbitalParts[kind] != null)
            return orbitalParts[kind];
        var b = new Builder();
        if (kind == 0)
        {
            b.Box(Vector3.zero, new Vector3(.52f, .76f, .4f), 15, 13);
            foreach (int s in new[] { -1, 1 })
                SolarPanel(b, new Vector3(s * .42f, 0, .04f), .34f, 1.18f);
            b.Drum(new Vector3(0, 0, -.3f), .19f, .16f, 10, 8, 13);
        }
        else if (kind == 1)
        {
            b.Drum(Vector3.zero, .36f, .4f, 15, 8, 6);
            foreach (int s in new[] { -1, 1 })
                b.Box(new Vector3(s * .16f, -.37f, -.08f), new Vector3(.16f, .65f, .24f), 11, 6);
            b.Drum(new Vector3(0, 0, -.32f), .17f, .1f, 10, 8);
        }
        else if (kind == 2)
        {
            b.Drum(Vector3.zero, .55f, .26f, 13, 12, 6);
            b.Ball(new Vector3(0, 0, -.24f), new Vector3(.75f, .75f, .46f), 10);
            b.Drum(new Vector3(0, 0, -.48f), .17f, .07f, 9, 10);
        }
        else
            b.Prism(new[] { V(-.58f, -.65f), V(-.68f, .32f), V(-.38f, .72f), V(-.04f, .60f), V(-.04f, -.60f) }, .18f, 7, Vector3.back * .64f, 15);
        return orbitalParts[kind] = b.Finish("Orbital breakable module " + kind);
    }
}
