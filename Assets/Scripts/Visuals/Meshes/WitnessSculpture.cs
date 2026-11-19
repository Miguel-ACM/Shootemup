using System.Collections.Generic;
using UnityEngine;

// Closed faceted sections and ridged blades share meshes between the mirrored
// joints. The vanes turn around their roots so the wing can fold like a fan.
public sealed class WitnessSculpture : MonoBehaviour
{
    static readonly Color Black = new Color(.072f, .064f, .098f), Slate = new Color(.15f, .14f, .20f),
        Edge = new Color(.29f, .28f, .34f), Silver = new Color(.71f, .69f, .76f), Iris = new Color(1, .26f, .33f);
    static Mesh body, face, pinion, train, crown, arm, gauntlet;
    static Material obsidian;
    public static Material Obsidian => obsidian != null ? obsidian : obsidian = new Material(Resources.Load<Shader>("WitnessObsidian"));
    public static Mesh Body => body != null ? body : body = Torso();

    sealed class Stone
    {
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<int> indices = new List<int>();
        readonly List<Color> paint = new List<Color>();
        public void Triangle(Vector3 a, Vector3 b, Vector3 c, Color color)
        {
            int n = verts.Count;
            verts.Add(a);
            verts.Add(b);
            verts.Add(c);
            indices.Add(n);
            indices.Add(n + 1);
            indices.Add(n + 2);
            paint.Add(color);
            paint.Add(color);
            paint.Add(color);
        }
        // Consecutive elliptical cross sections around a vertical spine.
        public void Loft(Vector3 centre, float[] y, float[] width, float[] depth, int sides, Color pigment)
        {
            Vector3 Point(int row, int side)
            {
                float angle = Mathf.PI * 2 * side / sides + (row % 2) * .10f;
                return centre + new Vector3(width[row] * Mathf.Cos(angle), y[row], depth[row] * Mathf.Sin(angle));
            }
            for (int j = 1; j < y.Length; j++)
                for (int k = 0; k < sides; k++)
                {
                    var a = Point(j - 1, k);
                    var b = Point(j - 1, k + 1);
                    var c = Point(j, k + 1);
                    var d = Point(j, k);
                    Triangle(a, b, d, pigment);
                    Triangle(b, c, d, pigment * ((k % 3 == 0) ? 1.10f : .93f));
                }
        }
        // A tapered, swept, double-edged blade with a raised middle ridge.
        public void Blade(Vector3 a, Vector3 bend, Vector3 tip, float width, Color pigment, int cuts = 7)
        {
            Vector3 left = a, right = a, ridge = a, back = a;
            for (int j = 1; j <= cuts; j++)
            {
                float t = j / (float)cuts, s = 1 - t;
                var p = s * s * a + 2 * s * t * bend + t * t * tip;
                var d = (s * (bend - a) + t * (tip - bend)).normalized;
                var cross = new Vector3(-d.y, d.x, 0).normalized;
                float w = j == cuts ? 0 : width * Mathf.Pow(Mathf.Sin(t * Mathf.PI), .70f) * (1.3f - .60f * t);
                var l = p - cross * w;
                var r = p + cross * w;
                var f = p + Vector3.back * (w * .83f);
                var b = p + Vector3.forward * (w * .43f);
                Triangle(left, l, ridge, pigment);
                Triangle(ridge, l, f, pigment * 1.15f);
                Triangle(ridge, f, r, pigment * .78f);
                Triangle(ridge, r, right, pigment * .89f);
                Triangle(left, back, l, pigment * .66f);
                Triangle(l, back, b, pigment);
                Triangle(back, right, r, pigment * .82f);
                Triangle(back, r, b, pigment * .72f);
                left = l;
                right = r;
                ridge = f;
                back = b;
            }
        }
        public Mesh Finish(string name)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetTriangles(indices, 0);
            mesh.SetColors(paint);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
    static Mesh Torso()
    {
        var s = new Stone();
        s.Loft(Vector3.zero, new[] { -1.3f, -.84f, -.48f, -.05f, .38f, .55f, .76f },
            new[] { .01f, .31f, .26f, .5f, .69f, .38f, .03f }, new[] { .01f, .19f, .23f, .37f, .30f, .20f, .02f }, 14, Black);
        for (int hand = -1; hand <= 1; hand += 2)
        {
            for (int i = 0; i < 5; i++)
            {
                float y = .41f - i * .14f;
                s.Blade(new Vector3(hand * (.64f - i * .066f), y, -.09f), new Vector3(hand * .27f, y - .25f, -.48f), new Vector3(0, y - .29f, -.38f), .074f, i == 0 ? Edge : Slate);
            }
            s.Blade(new Vector3(hand * .23f, .46f, -.06f), new Vector3(hand * .81f, .49f, -.08f), new Vector3(hand * 1.02f, .91f, .16f), .14f, Slate);
        }
        return s.Finish("Witness / ribbed carapace");
    }
    static Mesh Mask()
    {
        var s = new Stone();
        s.Loft(Vector3.zero, new[] { -.49f, -.22f, .05f, .25f, .40f }, new[] { .015f, .23f, .34f, .29f, .025f }, new[] { .02f, .22f, .28f, .19f, .02f }, 12, Slate);
        for (int h = -1; h <= 1; h += 2)
        {
            s.Blade(new Vector3(h * .015f, -.39f, -.19f), new Vector3(h * .25f, .02f, -.46f), new Vector3(h * .31f, .38f, -.10f), .135f, Black);
            s.Blade(new Vector3(h * .075f, .11f, -.30f), new Vector3(h * .34f, .42f, -.12f), new Vector3(h * .45f, .78f, .06f), .077f, Edge);
            s.Blade(new Vector3(h * .16f, .26f, 0), new Vector3(h * .51f, .19f, .08f), new Vector3(h * .69f, .43f, .16f), .08f, Black);
        }
        s.Blade(new Vector3(0, -.23f, -.31f), new Vector3(0, .04f, -.39f), new Vector3(0, .24f, -.29f), .027f, Iris);
        return s.Finish("Witness / split visor");
    }
    static Mesh Wing()
    {
        var s = new Stone();
        var root = Vector3.zero;
        var elbow = new Vector3(.94f, .54f, .12f);
        s.Blade(root, new Vector3(.36f, .69f, .16f), elbow, .18f, Slate);
        s.Blade(elbow, new Vector3(1.60f, .85f, .19f), new Vector3(1.93f, 1.35f, .13f), .10f, Edge);
        for (int j = 0; j < 8; j++)
        {
            float t = j / 7f;
            var start = new Vector3(.23f + 1.34f * t, .23f + .61f * t, .13f * t);
            var bend = start + new Vector3(.14f + .38f * t, -.50f + .14f * t, -.19f);
            var tip = start + new Vector3(.39f + .29f * t, -1.15f + .64f * t, .12f);
            s.Blade(start, bend, tip, .14f - .045f * t, j % 3 == 0 ? Slate : Black);
        }
        return s.Finish("Witness / vaulted pinion");
    }
    static Mesh Robe()
    {
        var s = new Stone();
        for (int j = -2; j <= 2; j++)
            s.Blade(new Vector3(j * .105f, 0, 0), new Vector3(j * .24f, -.78f, .23f), new Vector3(j * .31f, -1.55f + Mathf.Abs(j) * .23f, .14f), .19f, j % 2 == 0 ? Black : Slate);
        return s.Finish("Witness / trailing stole");
    }
    static Mesh Halo()
    {
        var s = new Stone();
        for (int i = 0; i < 36; i++)
        {
            if (i % 12 == 11)
                continue;
            float a = i * 2 * Mathf.PI / 36, b = (i + 1) * 2 * Mathf.PI / 36;
            Vector3 u = new Vector3(Mathf.Cos(a), Mathf.Sin(a)), v = new Vector3(Mathf.Cos(b), Mathf.Sin(b));
            s.Triangle(u * .87f, v * .87f, u, Slate);
            s.Triangle(u, v * .87f, v, Edge);
            s.Triangle(u, v, u * .976f + Vector3.back * .05f, Silver);
            if (i % 3 == 0)
                s.Blade(u * .93f, u * 1.06f, u * (i % 9 == 0 ? 1.30f : 1.15f), .034f, Silver, 3);
        }
        return s.Finish("Witness / interrupted nimbus");
    }
    static Mesh Forearm(bool hand)
    {
        var s = new Stone();
        if (!hand)
        {
            s.Blade(Vector3.zero, new Vector3(.17f, -.29f, -.11f), new Vector3(.45f, -.69f, 0), .14f, Slate);
            s.Blade(new Vector3(.08f, -.11f, -.08f), new Vector3(.23f, -.22f, -.14f), new Vector3(.47f, -.39f, -.05f), .055f, Edge);
        }
        else
        {
            s.Blade(Vector3.zero, new Vector3(.33f, -.13f, -.09f), new Vector3(.52f, -.32f, -.12f), .145f, Black);
            for (int j = 0; j < 4; j++)
            {
                var knuckle = new Vector3(.41f + j * .026f, -.20f - j * .045f, -.14f);
                s.Blade(knuckle, knuckle + new Vector3(.22f, -.05f, 0), knuckle + new Vector3(.28f - j * .04f, -.30f - .024f * j, .06f), .039f, Edge, 5);
            }
        }
        return s.Finish(hand ? "Witness / taloned hand" : "Witness / vambrace");
    }
    readonly Transform[] wings = new Transform[4];
    readonly Transform[] arms = new Transform[2], hands = new Transform[2];
    Transform mask, halo, robe;
    Transform Part(string name, Mesh mesh, Vector3 p)
    {
        var part = new GameObject(name);
        part.transform.SetParent(transform, false);
        part.transform.localPosition = p;
        part.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = part.AddComponent<MeshRenderer>();
        r.sortingLayerName = "Enemy";
        r.sharedMaterial = Obsidian;
        return part.transform;
    }
    public void Assemble()
    {
        if (face == null)
            face = Mask();
        if (pinion == null)
            pinion = Wing();
        if (train == null)
            train = Robe();
        if (crown == null)
            crown = Halo();
        if (arm == null)
            arm = Forearm(false);
        if (gauntlet == null)
            gauntlet = Forearm(true);
        halo = Part("Broken nimbus", crown, new Vector3(0, .92f, .49f));
        robe = Part("Split train", train, new Vector3(0, -.55f, .12f));
        mask = Part("Split visor", face, new Vector3(0, .55f, -.25f));
        for (int i = 0; i < 2; i++)
        {
            int h = i * 2 - 1;
            arms[i] = Part("Raised arm", arm, new Vector3(h * .51f, .26f, -.24f));
            arms[i].localScale = new Vector3(h, 1, 1);
            hands[i] = Part("Conducting hand", gauntlet, Vector3.zero);
            hands[i].SetParent(arms[i], false);
            hands[i].localPosition = new Vector3(.45f, -.65f, -.03f);
        }
        for (int i = 0; i < 4; i++)
        {
            int h = i % 2 == 0 ? -1 : 1;
            wings[i] = Part("Mantle pinion", pinion, new Vector3(h * .43f, i < 2 ? .35f : .04f, i < 2 ? .20f : .35f));
            wings[i].localScale = new Vector3(h, 1, 1) * (i < 2 ? 1 : .79f);
        }
    }
    public void Pose(float t, int phase, float strike)
    {
        for (int i = 0; i < 4; i++)
        {
            int h = i % 2 == 0 ? -1 : 1;
            float wave = Mathf.Sin(t * 1.42f - i / 2 * .83f);
            wings[i].localRotation = Quaternion.Euler(9 * wave, h * (18 * wave + 13 * strike), h * ((i < 2 ? 16 : -29) + 11 * wave + (phase - 1) * 3 + 12 * strike));
        }
        mask.localRotation = Quaternion.Euler(5 * Mathf.Cos(t), 15 * Mathf.Sin(t * .91f), 4 * Mathf.Sin(t * .73f));
        halo.localRotation = Quaternion.Euler(20 + 19 * Mathf.Sin(t * .49f), 14 * Mathf.Cos(t * .47f), t * (8 + 2 * phase));
        robe.localRotation = Quaternion.Euler(9 * Mathf.Sin(t * 1.4f), 0, 10 * Mathf.Sin(t * 1.2f));
        for (int i = 0; i < 2; i++)
        {
            int h = i * 2 - 1;
            float gesture = Mathf.Sin(t * (.9f + .11f * phase) + i * 1.8f);
            arms[i].localRotation = Quaternion.Euler(8 * gesture, 12 * h * gesture, h * (phase * 8 + 18 * gesture + 17 * strike));
            hands[i].localRotation = Quaternion.Euler(-9 * strike, 14 * gesture, 24 * gesture - 18 * strike);
        }
    }
}
