using UnityEngine;

public static partial class IndustrialMeshFactory
{
    // Atlas moth. Each wing is a cambered skin wrapped around a continuous leading
    // spar. The paint follows the span and chord, even where the trailing edge curls.
    static Mesh mothBody, mothLeft, mothRight, mothDisplay;
    static Material mothPaint;
    public static readonly Vector3 WardenLeftPivot = new Vector3(-.29f, -.08f, -.10f);
    public static readonly Vector3 WardenRightPivot = new Vector3(.29f, -.08f, -.10f);
    public static readonly Vector3 WardenLeftMuzzle = new Vector3(-2.03f, -.95f, -.31f);
    public static readonly Vector3 WardenRightMuzzle = new Vector3(2.03f, -.95f, -.31f);
    public static readonly Vector3 WardenNoseMuzzle = new Vector3(0, -1.42f, -.27f);
    public static Material MothPaint
    {
        get
        {
            if (mothPaint == null)
                mothPaint = new Material(CombatMaterials.Fleet)
                {
                    name = "Warden / lacquer and silk",
                    mainTexture = Resources.Load<Texture2D>("MothLivery")
                };
            return mothPaint;
        }
    }

    public static Mesh MushroomWardenMesh()
    {
        if (mothDisplay != null)
            return mothDisplay;
        mothDisplay = new Mesh
        {
            name = "Warden / open wings"
        };
        mothDisplay.CombineMeshes(new[] { new CombineInstance { mesh = MushroomWardenBodyMesh(), transform = Matrix4x4.identity }, new CombineInstance { mesh = MushroomWardenWingMesh(false), transform = Matrix4x4.Translate(WardenLeftPivot) }, new CombineInstance { mesh = MushroomWardenWingMesh(true), transform = Matrix4x4.Translate(WardenRightPivot) } });
        return mothDisplay;
    }

    public static Mesh MushroomWardenBodyMesh()
    {
        if (mothBody != null)
            return mothBody;
        var b = new Builder();
        // Overlapping abdominal tergites diminish towards a long dark tail.
        b.Pearl(new Vector3(0, .43f, -.08f), new Vector3(.28f, 1.04f, .21f), 11, 16, 10);
        for (int i = 0; i < 5; i++)
        {
            float y = .28f + i * .225f;
            float breadth = .29f - i * .037f;
            b.Pearl(new Vector3(0, y, -.15f), new Vector3(breadth, .245f, .215f - i * .023f), i % 2 == 0 ? 2 : 4, 16, 10);
            CutBlade(b, new[] { new Vector3(0, y - .09f, -.373f + i * .024f), new Vector3(0, y + .08f, -.35f + i * .024f), new Vector3(0, y + .21f, -.25f + i * .018f) }, .046f, .024f, 13);
        }

        b.Pearl(new Vector3(0, -.18f, -.17f), new Vector3(.48f, .57f, .32f), 11, 20, 12);
        b.Pearl(new Vector3(0, -.11f, -.29f), new Vector3(.42f, .47f, .30f), 14, 20, 12);
        // Raised, swept shoulder scales direct the eye into the narrow canopy.
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 P(float x, float y, float z) => new Vector3(s * x, y, z);
            for (int k = 0; k < 4; k++)
            {
                float f = k / 3f;
                CutBlade(b, new[] { P(.10f, -.47f + .15f * f, -.55f), P(.22f, -.32f + .27f * f, -.57f), P(.38f + .09f * f, -.15f + .38f * f, -.43f), P(.51f + .10f * f, -.01f + .38f * f, -.21f) }, .071f, .048f, k == 0 ? 15 : 1);
            }

            // Pectinate antennae: a curved stem with paired tapered combs.
            b.Sweep(P(.20f, -.75f, -.25f), P(.31f, -1.23f, -.39f), P(.70f, -1.51f, -.20f), P(.86f, -1.67f, -.14f), .038f, .034f, 13, false, 14);
            for (int k = 0; k < 7; k++)
            {
                float t = (k + 1) / 9f;
                Vector3 basePoint = P(.22f + .59f * t, -.89f - .73f * t, -.26f);
                float barb = .16f * Mathf.Sin(Mathf.PI * t);
                CutBlade(b, new[] { basePoint, basePoint + P(barb * .67f, .03f, -.012f), basePoint + P(barb, .11f, 0) }, .029f, .02f, 1);
            }
        }

        b.Pearl(new Vector3(0, -.74f, -.21f), new Vector3(.32f, .38f, .24f), 13, 18, 10);
        b.Pearl(new Vector3(0, -.71f, -.33f), new Vector3(.255f, .32f, .22f), 11, 18, 10);
        b.Pearl(new Vector3(0, -.71f, -.40f), new Vector3(.213f, .285f, .16f), 9, 18, 12);
        MothBarrel(b, WardenNoseMuzzle, .105f, .40f);
        b.MothTrimUV();
        return mothBody = b.Finish("Warden / antennae, canopy and segmented abdomen");
    }

    // A barrel is turned about its bore. Its open internal throat stops behind
    // the lip, so the launch marker stays on the physical exit as the wings bank.
    static void MothBarrel(Builder b, Vector3 exit, float radius, float length)
    {
        float[] bore =
        {
            .40f,
            .42f,
            .96f,
            1,
            .72f,
            .80f,
            1.04f,
            .60f,
            .35f
        };
        float[] at =
        {
            .13f,
            0,
            0,
            .12f,
            .19f,
            .31f,
            .46f,
            .80f,
            1
        };
        int[] paint =
        {
            11,
            11,
            13,
            1,
            11,
            13,
            4,
            2
        };
        for (int ring = 0; ring < paint.Length; ring++)
        {
            int band = ring;
            b.Surface(12, 1, (u, v) =>
            {
                float a = u * Mathf.PI * 2;
                float r = radius * Mathf.Lerp(bore[band], bore[band + 1], v);
                return exit + new Vector3(r * Mathf.Cos(a), length * Mathf.Lerp(at[band], at[band + 1], v), r * Mathf.Sin(a));
            }, paint[band]);
        }
    }

    // Each station specifies span, leading edge and trailing edge in hull space.
    static readonly Vector3[] Forewing =
    {
        new Vector3(.19f, -.54f, .37f),
        new Vector3(.56f, -.87f, .75f),
        new Vector3(1.10f, -1.01f, .89f),
        new Vector3(1.71f, -.94f, .68f),
        new Vector3(2.22f, -.71f, .29f),
        new Vector3(2.57f, -.57f, -.19f),
        new Vector3(2.85f, -.59f, -.56f)
    };
    static readonly Vector3[] Hindwing =
    {
        new Vector3(.24f, .06f, .50f),
        new Vector3(.54f, .14f, 1.25f),
        new Vector3(.90f, .29f, 1.74f),
        new Vector3(1.24f, .30f, 1.48f),
        new Vector3(1.54f, .21f, 1.51f),
        new Vector3(1.70f, .26f, 1.72f),
        new Vector3(1.82f, .59f, 1.79f),
        new Vector3(1.91f, 1.36f, 1.93f),
        new Vector3(1.87f, 2.17f, 2.20f)
    };
    static Vector3 WingSkin(bool fore, int side, float span, float chord)
    {
        var profile = fore ? Forewing : Hindwing;
        float station = span * (profile.Length - 1);
        int i = Mathf.Min(profile.Length - 2, (int)station);
        float t = station - i;
        Vector3 a = profile[Mathf.Max(0, i - 1)], b = profile[i], c = profile[i + 1], d = profile[Mathf.Min(i + 2, profile.Length - 1)];
        Vector3 edge = .5f * ((2 * b) + (-a + c) * t + (2 * a - 5 * b + 4 * c - d) * t * t + (-a + 3 * b - 3 * c + d) * t * t * t);
        float scallop = Mathf.Sin(span * Mathf.PI * (fore ? 6 : 5));
        float trailing = edge.z - .11f * scallop * scallop * Mathf.Sin(span * Mathf.PI);
        float camber = Mathf.Sin(chord * Mathf.PI) * Mathf.Sin(span * Mathf.PI);
        return new Vector3(side * edge.x, Mathf.Lerp(edge.y, trailing, chord), (fore ? -.26f : -.015f) - .18f * camber + .12f * span * span + .05f * chord * scallop);
    }

    public static Mesh MushroomWardenWingMesh(bool right)
    {
        Mesh cached = right ? mothRight : mothLeft;
        if (cached != null)
            return cached;
        int side = right ? 1 : -1;
        var b = new Builder();
        var exit = right ? WardenRightMuzzle : WardenLeftMuzzle;
        // The outboard gun is slung beneath the leading spar, with its forward
        // collar above the lacquer. The rear fairing sinks into the skin.
        MothBarrel(b, exit, .118f, .82f);
        for (int i = 0; i < 2; i++)
        {
            float t = i == 0 ? .06f : .52f;
            Vector3 from = WingSkin(true, side, t, .07f), to = WingSkin(true, side, t + .41f, .05f);
            b.Sweep(from, Vector3.Lerp(from, to, .34f) + Vector3.down * .09f, Vector3.Lerp(from, to, .67f) + Vector3.down * .04f, to, .041f, .043f, 13, false, 12);
        }

        b.MothTrimUV();
        b.MothPanel(false, side);
        b.MothPanel(true, side);
        Mesh wing = b.Finish(right ? "Warden / starboard silk" : "Warden / port silk");
        Vector3 pivot = right ? WardenRightPivot : WardenLeftPivot;
        var vertices = wing.vertices;
        for (int i = 0; i < vertices.Length; i++)
            vertices[i] -= pivot;
        wing.vertices = vertices;
        wing.RecalculateBounds();
        if (right)
            mothRight = wing;
        else
            mothLeft = wing;
        return wing;
    }

    sealed partial class Builder
    {
        public void MothTrimUV()
        {
            for (int i = 0; i < uv.Count; i++)
            {
                int pigment = Mathf.FloorToInt(uv[i].x * 4) + 4 * Mathf.FloorToInt(uv[i].y * 4);
                uv[i] = new Vector2((pigment + .5f) / 16f, .035f);
            }
        }

        public void MothPanel(bool fore, int side)
        {
            const int spans = 40, chords = 10;
            int start = vertices.Count;
            for (int face = 0; face < 2; face++)
                for (int row = 0; row <= chords; row++)
                    for (int column = 0; column <= spans; column++)
                    {
                        float u = column / (float)spans, v = row / (float)chords;
                        Vector3 p = WingSkin(fore, side, u, v);
                        p.z += face * (.025f + .09f * Mathf.Sin(Mathf.PI * v) * (1 - u));
                        vertices.Add(p);
                        uv.Add(new Vector2(.004f + .992f * u, fore ? Mathf.Lerp(.99f, .515f, v) : Mathf.Lerp(.485f, .095f, v)));
                        if (column == spans || row == chords)
                            continue;
                        int a = vertices.Count - 1, c = a + spans + 1;
                        MothQuad(a, c, c + 1, a + 1, (side < 0) != (face == 1));
                    }

            int offset = (spans + 1) * (chords + 1);
            for (int j = 0; j < spans; j++)
            {
                int a = start + j, b = a + chords * (spans + 1);
                MothQuad(a, a + 1, a + 1 + offset, a + offset, side < 0);
                MothQuad(b, b + offset, b + 1 + offset, b + 1, side < 0);
            }
        }

        void MothQuad(int a, int b, int c, int d, bool flip)
        {
            indices.Add(a);
            indices.Add(flip ? c : b);
            indices.Add(flip ? b : c);
            indices.Add(a);
            indices.Add(flip ? d : c);
            indices.Add(flip ? c : d);
        }
    }
}
