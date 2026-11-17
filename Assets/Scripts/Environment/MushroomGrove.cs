using System.Collections.Generic;
using UnityEngine;

// An overhead woodland, drawn as one depth-writing mesh per scrolling section.
// Trees on opposite sides of a join share the same 80-unit lighting and planting plan.
public static class MushroomGrove
{
    const int Sections = 4;
    const float Tau = 2 * Mathf.PI;
    static readonly Vector2 Sun = new Vector2(-.56f, .83f);
    static readonly Vector2 NoLeaf = new Vector2(-1, -1);
    struct TreeSite
    {
        public Vector2 center;
        public float size;
        public bool fir, silver;
        public int seed, section;
    }

    public static Mesh Build(int seed, int section, float length)
    {
        var grove = new WoodMesh();
        float circuit = Sections * length;
        var layout = new List<TreeSite>(180);
        var plan = new System.Random(unchecked(seed * 479 + 1601));
        float phase = Between(plan, 0, Tau);
        // Two staggered woodland clearings per section; older trees continue into
        // the gaps between them. The flight path crosses the groves at different angles.
        var clearings = new Vector2[8];
        for (int i = 0; i < clearings.Length; i++)
            clearings[i] = new Vector2(2.9f * Mathf.Sin(phase + i * 2.24f) + Between(plan, -.45f, .45f), (i + .38f) * circuit / 8 - length * .5f + Between(plan, -1.1f, 1.1f));
        for (int tile = 0; tile < Sections; tile++)
            for (int row = 0; row < 10; row++)
                for (int col = 0; col < 7; col++)
                {
                    Vector2 p = new Vector2(-6.9f + (col + .5f * (row % 2)) * 2.15f + Between(plan, -.60f, .60f), (tile - .5f) * length + (row + .5f) * length / 10 + Between(plan, -.66f, .66f));
                    float nearest = 1000;
                    foreach (var glade in clearings)
                    {
                        Vector2 v = Delta(p, glade, circuit);
                        v.y *= .78f;
                        nearest = Mathf.Min(nearest, v.sqrMagnitude);
                    }

                    if (nearest < 2.55f * 2.55f || plan.NextDouble() < .12)
                        continue;
                    float stand = Mathf.Sin(p.x * .57f + p.y * Tau / circuit * 3 + phase);
                    layout.Add(new TreeSite { center = p, size = Between(plan, .80f, 1.58f), fir = stand > .38f, silver = stand < -.43f, seed = plan.Next(), section = tile });
                }

        float origin = section * length;
        const int columns = 36, rows = 44;
        for (int row = 0; row <= rows; row++)
            for (int col = 0; col <= columns; col++)
            {
                float x = col == 0 ? -19 : col == columns ? 19 : Mathf.Lerp(-8, 8, (col - 1f) / (columns - 2));
                float y = Mathf.Lerp(-length * .5f, length * .5f, row / (float)rows);
                Vector2 p = new Vector2(x, y + origin);
                float shadow = 0;
                foreach (var site in layout)
                {
                    Vector2 d = Delta(p, site.center - Sun * site.size * .48f, circuit);
                    shadow = Mathf.Max(shadow, 1 - Mathf.Clamp01(d.sqrMagnitude / (2.05f * site.size * site.size)));
                }

                // Peat in the deep shade, with gold-green moss on the raised banks.
                float bed = Mathf.Sin(x * .84f + .85f * Mathf.Sin(p.y * Tau * 4 / circuit + phase)) * .5f + Mathf.Cos(p.y * Tau * 7 / circuit + x * .70f) * .22f;
                Color moss = Color.Lerp(new Color(.135f, .149f, .107f), new Color(.29f, .34f, .173f), Mathf.SmoothStep(0, 1, .51f + bed * .69f));
                Color paint = Color.Lerp(moss, new Color(.059f, .105f, .108f), .88f * shadow);
                int vertex = grove.Add(new Vector2(x, y), -.015f, paint);
                if (row < rows && col < columns)
                    grove.Quad(vertex, vertex + columns + 1, vertex + columns + 2, vertex + 1);
            }

        foreach (var site in layout)
        {
            if (site.section != section)
                continue;
            var local = site.center - Vector2.up * origin;
            var branch = new System.Random(site.seed);
            if (site.fir)
                Fir(grove, local, site.size, branch);
            else
                Broadleaf(grove, local, site.size, site.silver, branch);
        }

        var flora = new System.Random(unchecked(seed * 571 + section * 97003 + 377));
        for (int i = section * 2; i < section * 2 + 2; i++)
        {
            Vector2 center = clearings[i] - Vector2.up * origin;
            float direction = Between(flora, 0, Tau);
            Vector2 u = Unit(direction);
            // Fungi follow the decaying timber and the moist edges of the opening.
            NurseLog(grove, center - u * .35f + Turn(u) * .73f, u, Between(flora, 1.60f, 2.65f), flora);
            for (int patch = 0; patch < 4; patch++)
            {
                float a = direction + patch * 1.58f + Between(flora, -.24f, .24f);
                Vector2 colony = center + Unit(a) * Between(flora, .94f, 2.00f);
                bool violet = (i + patch) % 4 == 1;
                float mainRadius = Between(flora, .32f, .53f);
                Toadstool(grove, colony, mainRadius, violet, flora);
                int young = 2 + flora.Next(3);
                for (int bud = 0; bud < young; bud++)
                {
                    Vector2 offset = Unit(a + Tau * bud / young + Between(flora, -.27f, .27f)) * Between(flora, mainRadius + .22f, mainRadius + .54f);
                    Toadstool(grove, colony + offset, Between(flora, .11f, .24f), violet, flora);
                }
            }
        }

        for (int i = 0; i < 62; i++)
        {
            Vector2 p = new Vector2(Between(flora, -6.5f, 6.5f), Between(flora, -length * .5f, length * .5f));
            Fern(grove, p, Between(flora, .17f, .42f), flora);
        }

        return grove.Finish();
    }

    static Vector2 Delta(Vector2 a, Vector2 b, float period)
    {
        Vector2 d = a - b;
        d.y -= Mathf.Round(d.y / period) * period;
        return d;
    }

    static Vector2 Unit(float a) => new Vector2(Mathf.Cos(a), Mathf.Sin(a));
    static Vector2 Turn(Vector2 p) => new Vector2(-p.y, p.x);
    static float Between(System.Random random, float low, float high) => Mathf.Lerp(low, high, (float)random.NextDouble());
    static Vector3 Place(Vector2 xy, float height) => new Vector3(xy.x, xy.y, -height);
    static Color Light(Color ink, float value) => new Color(ink.r * value, ink.g * value, ink.b * value, 1);
    static void Broadleaf(WoodMesh mesh, Vector2 root, float radius, bool silver, System.Random rng)
    {
        Color bark = silver ? new Color(.45f, .42f, .32f) : new Color(.28f, .215f, .15f);
        Color leaves = silver ? new Color(.67f, .84f, .63f) : new Color(.60f, .77f, .53f);
        Vector2 fork = root + Unit(Between(rng, 0, Tau)) * radius * .21f;
        var trunk = Place(fork, radius * .60f);
        mesh.Limb(Place(root, .04f), trunk, radius * .145f, radius * .10f, bark);
        float rotation = Between(rng, 0, Tau);
        int arms = silver ? 5 : 7;
        for (int i = 0; i < arms; i++)
        {
            Vector2 axis = Unit(rotation + Tau * i / arms + Between(rng, -.27f, .27f));
            float reach = radius * Between(rng, .78f, 1.15f);
            Vector2 elbow = fork + axis * reach * .49f + Turn(axis) * Between(rng, -.13f, .13f);
            var joint = Place(elbow, radius * .83f);
            mesh.Limb(Place(root + axis * (radius * .47f), .028f), Place(root, radius * .15f), .023f * radius, .13f * radius, bark);
            mesh.Limb(trunk, joint, .09f * radius, .055f * radius, bark);
            for (int twig = -1; twig <= 1; twig += 2)
            {
                Vector2 tip = elbow + (axis + Turn(axis) * twig * .54f) * (reach * .44f);
                float height = radius * Between(rng, .97f, 1.17f);
                mesh.Limb(joint, Place(tip, height), .056f * radius, .011f * radius, bark);
                float light = .74f + .15f * Vector2.Dot(axis, Sun) + Between(rng, -.05f, .10f);
                // Unequal fork heights leave occasional views into the bark.
                mesh.Leaves(tip, height, reach * 1.18f, reach * 1.03f, rotation + i * 2 + twig, Light(leaves, light), silver ? 1 : 0);
                mesh.Leaves(Vector2.Lerp(elbow, tip, .34f), height + .087f, radius * .89f, radius * .83f, rotation + twig * 2, Light(leaves, light * 1.12f), silver ? 1 : 0);
            }
        }

        mesh.Leaves(fork + Sun * radius * .16f, 1.25f * radius, radius * 1.84f, radius * 1.50f, rotation, Light(leaves, 1.17f), silver ? 1 : 0);
    }

    static void Fir(WoodMesh mesh, Vector2 root, float radius, System.Random rng)
    {
        Color needles = new Color(.19f, .37f, .275f);
        Vector2 tip = root + new Vector2(-.12f, .18f) * radius;
        mesh.Limb(Place(root, .03f), Place(tip, 1.52f * radius), .11f * radius, .025f * radius, new Color(.26f, .22f, .17f));
        float angle = Between(rng, 0, Tau);
        for (int tier = 0; tier < 4; tier++)
        {
            float size = radius * (1.12f - .235f * tier);
            float height = radius * (.45f + .345f * tier);
            Vector2 joint = Vector2.Lerp(root, tip, (tier + 1) * .23f);
            for (int branch = 0; branch < 7; branch++)
            {
                float a = angle + branch * Tau / 7 + tier * .54f + Between(rng, -.12f, .12f);
                Vector2 axis = Unit(a), across = Turn(axis);
                float reach = size * Between(rng, .84f, 1.1f);
                Vector2 end = joint + axis * reach;
                float exposure = .57f + .24f * Vector2.Dot(axis, Sun) + tier * .105f;
                // Needles hang off both sides of each bough in swept sprays.
                mesh.Limb(Place(joint, height), Place(end, height - reach * .24f), reach * .042f, reach * .007f, Light(needles, exposure * .54f));
                for (int k = 0; k < 5; k++)
                {
                    float t = (k + .35f) / 5;
                    float width = reach * (.27f * (1 - t) + .07f);
                    Vector2 p = joint + axis * reach * t;
                    float z = height - t * reach * .24f;
                    for (int flank = -1; flank <= 1; flank += 2)
                    {
                        Vector2 spray = p + across * (flank * width) + axis * reach * .25f;
                        mesh.Spear(p, spray, width * .42f, z, z - reach * .08f, Light(needles, exposure * (flank < 0 ? .91f : 1.05f)));
                    }
                }
            }
        }
    }

    static void Fern(WoodMesh mesh, Vector2 p, float reach, System.Random rng)
    {
        float twist = Between(rng, 0, Tau);
        Color leaf = new Color(.25f, .32f, .16f);
        for (int i = 0; i < 6; i++)
        {
            Vector2 axis = Unit(twist + i * Tau / 6);
            for (int leafPair = 1; leafPair <= 4; leafPair++)
            {
                float t = leafPair * .18f, width = reach * (.23f - .14f * t);
                Vector2 stem = p + axis * (t * reach);
                for (int s = -1; s <= 1; s += 2)
                    mesh.Spear(stem, stem + axis * (reach * .25f) + Turn(axis) * (s * width), width * .38f, .047f, .059f, Light(leaf, .84f + t * .30f));
            }
        }
    }

    static void NurseLog(WoodMesh mesh, Vector2 center, Vector2 axis, float length, System.Random rng)
    {
        Vector2 across = Turn(axis);
        Vector2 start = center - axis * length * .5f, end = center + axis * length * .5f;
        float radius = Between(rng, .115f, .19f);
        Color wood = new Color(.34f, .28f, .21f);
        mesh.Limb(Place(start, radius + .025f), Place(end, radius + .03f), radius, radius * .81f, wood);
        for (int i = 0; i < 5; i++)
        {
            Vector2 splinter = end + across * Between(rng, -radius * .7f, radius * .7f);
            mesh.Spear(splinter - axis * radius * .4f, splinter + axis * Between(rng, .07f, .26f), .027f, radius * 1.7f, radius * .92f, new Color(.55f, .405f, .23f));
        }

        for (int i = 0; i < 4; i++)
        {
            Vector2 p = Vector2.Lerp(start, end, (i + .65f) / 5);
            int sign = i % 2 == 0 ? 1 : -1;
            Vector2 elbow = p + (across * sign + axis * .28f) * Between(rng, .23f, .52f);
            mesh.Limb(Place(p, radius), Place(elbow, .17f), .071f, .034f, wood);
            mesh.Limb(Place(elbow, .17f), Place(elbow + (axis + across * .31f * sign) * .29f, .125f), .034f, .006f, wood);
            mesh.Leaves(p + across * (radius * .23f), radius * 1.92f, .26f, .17f, i * 2.1f, new Color(.78f, .79f, .44f), 1);
        }
    }

    static void Toadstool(WoodMesh mesh, Vector2 root, float radius, bool violet, System.Random rng)
    {
        float rotate = Between(rng, 0, Tau);
        Vector2 lean = Unit(rotate), lateral = Turn(lean);
        var cap = root + lean * (radius * .79f);
        float lip = radius * (violet ? .87f : .75f) + .065f;
        Color stem = violet ? new Color(.53f, .40f, .56f) : new Color(.68f, .62f, .45f);
        mesh.Shadow(cap - Sun * radius * .42f, radius);
        Vector2 stipe = Vector2.Lerp(root, cap, .58f) - lateral * radius * .075f;
        mesh.Limb(Place(root, .018f), Place(stipe, lip * .57f), radius * .22f, radius * .16f, stem);
        mesh.Limb(Place(stipe, lip * .57f), Place(cap, lip), radius * .16f, radius * .12f, stem);
        float crinkle = Between(rng, 0, Tau);
        Vector3 Surface(float r, float a)
        {
            Vector2 v = lean * (.79f * Mathf.Cos(a)) + lateral * Mathf.Sin(a);
            float ripple = 1 + .031f * Mathf.Cos(3 * a + crinkle) + .021f * Mathf.Sin(5 * a - crinkle);
            if (violet)
                ripple += .058f * Mathf.Cos(9 * a) * r * r;
            float crown = violet ? Mathf.Pow(1 - r, 1.57f) : Mathf.Sqrt(Mathf.Max(0, 1 - r * r));
            float z = lip + radius * ((violet ? .82f : .46f) * crown - .32f * r * Mathf.Cos(a));
            return Place(cap + v * (radius * r * ripple) + lean * (radius * crown * .24f), z);
        }

        Color skin = violet ? new Color(.46f, .28f, .53f) : new Color(.64f, .155f, .105f);
        const int segments = 20;
        int start = mesh.Count;
        for (int row = 0; row <= 6; row++)
        {
            float r = Mathf.Sin(row * Mathf.PI / 12);
            for (int i = 0; i < segments; i++)
            {
                float a = i * Tau / segments;
                Vector2 outward = lean * Mathf.Cos(a) + lateral * Mathf.Sin(a);
                float sun = .86f + .24f * Vector2.Dot(outward, Sun) * r + .12f * (1 - r);
                if (violet)
                    sun += .19f * Mathf.Cos(9 * a) * r * r;
                mesh.Add(Surface(r, a), Light(skin, sun));
                int v = start + row * segments + i, next = start + row * segments + (i + 1) % segments;
                if (row < 6)
                    mesh.Quad(v, next, next + segments, v + segments);
            }
        }

        for (int i = 0; i < segments; i++)
        {
            float a = i * Tau / segments, b = (i + 1) * Tau / segments;
            var rim = Surface(1, a);
            var rimNext = Surface(1, b);
            Vector3 gillOffset = -(Vector3)lean * (radius * .15f) + Vector3.forward * radius * .14f;
            var under = Vector3.Lerp(Place(cap, lip - radius * .22f), rim, .97f) + gillOffset;
            var underNext = Vector3.Lerp(Place(cap, lip - radius * .22f), rimNext, .97f) + gillOffset;
            int v = mesh.Add(rim, Light(skin, .62f));
            mesh.Add(rimNext, Light(skin, .62f));
            Color gills = Light(stem, i % 2 == 0 ? .86f : .65f);
            mesh.Add(underNext, gills);
            mesh.Add(under, gills);
            mesh.Quad(v, v + 1, v + 2, v + 3);
        }

        if (violet)
            return;
        int spots = radius > .28f ? 13 : 6;
        float shift = Between(rng, 0, Tau);
        for (int i = 0; i < spots; i++)
        {
            float a = shift + 2.39996f * i + Between(rng, -.19f, .19f);
            float r = .86f * Mathf.Sqrt((i + .45f) / spots);
            Vector3 pos = Surface(r, a) + Vector3.back * (.011f + radius * .032f);
            float w = radius * Between(rng, .058f, .117f);
            Color ivory = new Color(.86f, .81f, .64f);
            int top = mesh.Add(pos + Vector3.back * w * .31f, ivory);
            for (int j = 0; j < 5; j++)
            {
                Vector2 xy = Unit(a + j * Tau / 5) * w;
                mesh.Add(pos + new Vector3(xy.x, xy.y * .83f, -.003f), Light(ivory, .92f));
            }

            for (int j = 0; j < 5; j++)
                mesh.Triangle(top, top + 1 + j, top + 1 + (j + 1) % 5);
        }
    }

    sealed class WoodMesh
    {
        readonly List<Vector3> vertices = new List<Vector3>(32000);
        readonly List<Color32> colors = new List<Color32>(32000);
        readonly List<Vector2> uv = new List<Vector2>(32000);
        readonly List<int> indices = new List<int>(96000);
        public int Count => vertices.Count;

        public int Add(Vector2 p, float h, Color c) => Add(Place(p, h), c);
        public int Add(Vector3 p, Color c) => Vertex(p, c, NoLeaf);
        int Vertex(Vector3 p, Color c, Vector2 texture)
        {
            int i = vertices.Count;
            vertices.Add(p);
            colors.Add(c);
            uv.Add(texture);
            return i;
        }

        public void Triangle(int a, int b, int c)
        {
            indices.Add(a);
            indices.Add(b);
            indices.Add(c);
        }

        public void Quad(int a, int b, int c, int d)
        {
            Triangle(a, b, c);
            Triangle(a, c, d);
        }

        public void Shadow(Vector2 center, float r)
        {
            Color umber = new Color(.041f, .068f, .061f, .54f);
            int start = Add(center, .005f, umber);
            for (int ring = 1; ring <= 2; ring++)
                for (int i = 0; i < 12; i++)
                {
                    Vector2 delta = Unit(Tau * i / 12) * r * (ring == 1 ? .73f : 1.16f);
                    Color c = umber;
                    c.a = ring == 1 ? .45f : 0;
                    Add(center + delta, .005f, c);
                    int a = start + 1 + i, b = start + 1 + (i + 1) % 12;
                    if (ring == 1)
                        Triangle(start, a, b);
                    else
                        Quad(a, b, b + 12, a + 12);
                }
        }

        public void Leaves(Vector2 center, float h, float width, float length, float angle, Color tint, int kind)
        {
            Vector2 u = Unit(angle) * (width * .5f), v = Turn(Unit(angle)) * (length * .5f);
            Vector2 atlas = new Vector2((kind % 2) * .5f, (kind / 2) * .5f);
            int i = Vertex(Place(center - u - v, h - .069f), Light(tint, .92f), atlas + new Vector2(.005f, .005f));
            Vertex(Place(center + u - v, h + .039f), tint, atlas + new Vector2(.495f, .005f));
            Vertex(Place(center + u + v, h + .050f), tint, atlas + new Vector2(.495f, .495f));
            Vertex(Place(center - u + v, h - .042f), Light(tint, 1.06f), atlas + new Vector2(.005f, .495f));
            Quad(i, i + 1, i + 2, i + 3);
        }

        public void Spear(Vector2 basePoint, Vector2 end, float width, float low, float high, Color tint)
        {
            Vector2 side = Turn((end - basePoint).normalized) * width, mid = Vector2.Lerp(basePoint, end, .42f);
            int i = Add(basePoint, low, Light(tint, .73f));
            Add(mid + side, (low + high) * .5f, tint);
            Add(end, high, tint);
            Add(mid - side, (low + high) * .5f, Light(tint, .82f));
            Add(mid, (low + high) * .5f + .028f, Light(tint, 1.15f));
            Triangle(i, i + 1, i + 4);
            Triangle(i + 1, i + 2, i + 4);
            Triangle(i + 2, i + 3, i + 4);
            Triangle(i + 3, i, i + 4);
        }

        public void Limb(Vector3 from, Vector3 to, float baseRadius, float tipRadius, Color tint)
        {
            Vector3 axis = (to - from).normalized;
            Vector3 u = Vector3.Cross(axis, Mathf.Abs(axis.z) > .9f ? Vector3.right : Vector3.back).normalized;
            Vector3 v = Vector3.Cross(axis, u);
            int start = Count;
            for (int i = 0; i < 7; i++)
            {
                float a = Tau * i / 7;
                Vector3 normal = u * Mathf.Cos(a) + v * Mathf.Sin(a);
                float light = .78f + .32f * Vector3.Dot(normal, new Vector3(-.48f, .55f, -.68f));
                Add(from + normal * baseRadius, Light(tint, light));
                Add(to + normal * tipRadius, Light(tint, light * 1.10f));
                int n = (i + 1) % 7;
                Quad(start + 2 * i, start + 2 * n, start + 2 * n + 1, start + 2 * i + 1);
            }
        }

        public Mesh Finish()
        {
            var mesh = new Mesh
            {
                name = "Mushroom / overhead woodland"
            };
            if (Count > 65535)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
