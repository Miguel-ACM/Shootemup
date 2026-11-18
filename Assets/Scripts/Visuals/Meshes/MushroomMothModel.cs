using UnityEngine;

public static partial class IndustrialMeshFactory
{
    // The livery follows the broad folded panels all the way into the hooked tails.
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
        // A single tapered abdomen. Alternating wide plates and narrow collars
        // catch the light along a common keel.
        Vector3 Plate(float y, float width, int side)
        {
            float angle = side * Mathf.PI / 3;
            return new Vector3(width * Mathf.Sin(angle), y, -.10f - width * .87f * Mathf.Cos(angle));
        }
        for (int plate = 0; plate < 4; plate++)
        {
            float y = .34f + .28f * plate, width = .29f - .049f * plate;
            for (int side = 0; side < 6; side++)
            {
                Vector3 baseLeft = Plate(y, width, side), baseRight = Plate(y, width, side + 1);
                Vector3 rimLeft = Plate(y + .21f, width * .68f, side), rimRight = Plate(y + .21f, width * .68f, side + 1);
                b.Face(plate % 2 == 0 ? 2 : 4, baseLeft, rimLeft, rimRight, baseRight);
                if (plate == 3)
                    b.Face(13, rimLeft, new Vector3(0, 1.50f, -.10f), rimRight);
                else
                    b.Face(11, rimLeft, Plate(y + .28f, width - .049f, side), Plate(y + .28f, width - .049f, side + 1), rimRight);
            }
        }

        // A scalloped fly-agaric mantle straddles the wing hinges. The lifted rim
        // exposes alternating ivory gills; the narrow keel remains visible below it.
        const int pleats = 12;
        Vector3 Mantle(int i, float radius, float height)
        {
            float a = i * (2 * Mathf.PI / pleats);
            float scallop = (i & 1) == 0 ? 1 : .89f;
            return new Vector3(.83f * radius * scallop * Mathf.Cos(a),
                .15f + .61f * radius * scallop * Mathf.Sin(a), height);
        }
        Vector3 peak = new Vector3(0,.19f,-.73f);
        Vector3 throat = new Vector3(0,-.27f,-.32f);
        for (int i = 0; i < pleats; i++)
        {
            int next = i + 1;
            Vector3 shoulder = Mantle(i,.62f,-.64f), edge = Mantle(i,1,-.47f);
            Vector3 shoulderNext = Mantle(next,.62f,-.64f), edgeNext = Mantle(next,1,-.47f);
            b.Face(i % 3 == 0 ? 12 : 5, peak, shoulderNext, shoulder);
            b.Face(5, shoulder, shoulderNext, edgeNext, edge);
            b.Face(1, edge, edgeNext, Mantle(next,1,-.37f), Mantle(i,1,-.37f));
            b.Face((i & 1) == 0 ? 15 : 13, Mantle(i,1,-.37f), Mantle(next,1,-.37f), throat);
            if ((i & 1) == 0)
            {
                Vector3 spot = Mantle(i, .48f, -.67f);
                b.Face(15, spot + Vector3.left*.054f, spot + Vector3.up*.050f + Vector3.back*.013f,
                    spot + Vector3.right*.062f, spot + Vector3.down*.041f + Vector3.back*.010f);
            }
        }
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 P(float x, float y, float z) => new Vector3(s * x, y, z);
            // Pectinate antennae: a curved stem with paired tapered combs.
            Vector3 root = P(.20f, -.75f, -.25f), bend = P(.41f, -1.24f, -.31f), tip = P(.86f, -1.67f, -.14f);
            CutBlade(b, new[] { root, bend, tip }, .046f, .035f, 13);
            for (int k = 0; k < 5; k++)
            {
                float t = (k + 1) / 6f;
                Vector3 stem = t < .5f ? Vector3.Lerp(root, bend, t*2) : Vector3.Lerp(bend, tip, t*2-1);
                Vector3 tooth = stem + P(.15f * Mathf.Sin(Mathf.PI*t), .09f, -.026f);
                Vector3 heel = stem + P(.035f, -.056f, 0);
                if (s > 0) b.Face(1, stem, tooth, heel);
                else b.Face(1, stem, heel, tooth);
            }
        }

        CutStone(b, new Vector3(0, -.73f, -.22f), new Vector3(.31f, .39f, .20f), 13);
        CutStone(b, new Vector3(0, -.71f, -.40f), new Vector3(.24f, .32f, .16f), 9);
        b.Face(15, new Vector3(-.10f, -.64f, -.562f), new Vector3(-.037f, -.553f, -.562f),
            new Vector3(.036f, -.553f, -.562f), new Vector3(-.077f, -.65f, -.562f));
        MothBarrel(b, WardenNoseMuzzle, .105f, .40f);
        b.MothTrimUV();
        return mothBody = b.Finish("Warden / antennae, canopy and segmented abdomen");
    }

    // A barrel is turned about its bore. Its open internal throat stops behind
    // the lip, so the launch marker stays on the physical exit as the wings bank.
    static void MothBarrel(Builder b, Vector3 exit, float radius, float length)
    {
        float[] bore = { .42f, .42f, 1, .84f, .40f };
        float[] at = { .16f, 0, 0, .18f, 1 };
        int[] paint = { 11, 1, 13, 4 };
        for (int ring = 0; ring < paint.Length; ring++)
        {
            int band = ring;
            b.Surface(5, 1, (u, v) =>
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
        new Vector3(2.65f, -.78f, -.24f),
        new Vector3(2.91f, -1.04f, -1.00f)
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
        new Vector3(1.71f, 1.29f, 2.06f),
        new Vector3(1.43f, 2.50f, 2.53f)
    };
    public static Mesh MushroomWardenWingMesh(bool right)
    {
        Mesh cached = right ? mothRight : mothLeft;
        if (cached != null)
            return cached;
        int side = right ? 1 : -1;
        var b = new Builder();
        var exit = right ? WardenRightMuzzle : WardenLeftMuzzle;
        // The rear fairing sinks into the leading edge of the silk.
        MothBarrel(b, exit, .118f, .82f);

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
            // Two planes meet at the crease. A flat underside and thin edge keep
            // a solid silhouette as the wing banks. Adjacent facets share paint coordinates.
            var profile = fore ? Forewing : Hindwing;
            void Vertex(int station, int corner)
            {
                Vector3 edge = profile[station];
                float span = station / (float)(profile.Length - 1);
                float chord = corner == 1 ? .38f : (corner == 2 || corner == 3) ? 1 : 0;
                float depth = (fore ? -.26f : -.015f) + .12f * span * span;
                if (corner == 1) depth -= .22f * Mathf.Sin(Mathf.PI * span);
                if (corner >= 3) depth += .045f;
                vertices.Add(new Vector3(side * edge.x, Mathf.Lerp(edge.y, edge.z, chord), depth));
                uv.Add(new Vector2(Mathf.Lerp(.004f, .996f, span),
                    fore ? Mathf.Lerp(.99f, .515f, chord) : Mathf.Lerp(.485f, .095f, chord)));
            }
            for (int station = 0; station < profile.Length - 1; station++)
            {
                for (int corner = 0; corner < 5; corner++)
                {
                    int start = vertices.Count, next = (corner + 1) % 5;
                    Vertex(station, corner);
                    Vertex(station, next);
                    Vertex(station + 1, next);
                    Vertex(station + 1, corner);
                    MothQuad(start, start + 1, start + 2, start + 3, side < 0);
                }
            }
            // Close the cut edges exposed at the hinge and the hooked tip during a bank.
            for (int end = 0; end < 2; end++)
            {
                int station = end * (profile.Length - 1), start = vertices.Count;
                Vertex(station, 0);
                Vertex(station, 4);
                Vertex(station, 3);
                Vertex(station, 2);
                MothQuad(start, start + 1, start + 2, start + 3, (side < 0) != (end == 1));
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
