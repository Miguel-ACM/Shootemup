using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class CandylandBackdrop
{
    public const string VillageMeshName = "Candyland cottages and candy groves";
    const float HalfWidth = 22;
    static readonly Color Strawberry = new Color(.79f, .35f, .55f);
    static readonly Color Rose = new Color(.58f, .21f, .39f);
    static readonly Color SugarLane = new Color(.68f, .40f, .52f);
    static readonly Color Icing = new Color(.94f, .76f, .75f);
    static readonly Color Peppermint = new Color(.72f, .08f, .23f);
    static readonly Color Biscuit = new Color(.59f, .32f, .19f);
    static readonly Color Cocoa = new Color(.28f, .105f, .088f);
    static readonly Color MilkChocolate = new Color(.45f, .22f, .16f);
    static readonly Color BerryGlass = new Color(.29f, .07f, .17f);
    static readonly Color DistanceTint = new Color(.43f, .25f, .38f);
    static readonly Color[] Gumdrops =
    {
        new Color(.76f, .22f, .40f), new Color(.62f, .34f, .67f),
        new Color(.91f, .49f, .49f), new Color(.93f, .64f, .39f)
    };

    static Color Shade(Color color, float light) => new Color(color.r * light, color.g * light, color.b * light, color.a);

    static Color Distant(Color color)
    {
        var softened = Color.Lerp(Shade(color, .70f), DistanceTint, .22f);
        softened.a = color.a;
        return softened;
    }

    readonly int seed;
    readonly float phase;

    public CandylandBackdrop(int seed)
    {
        this.seed = seed;
        phase = (float)new System.Random(seed).NextDouble() * Mathf.PI * 2;
    }

    float Lane(float turn) => 1.42f * Mathf.Sin(turn * 3 + phase) + .72f * Mathf.Sin(turn * 5 - phase);
    float LaneWidth(float turn) => .79f + .11f * Mathf.Cos(turn * 4 + phase);

    static float Turn(float distance, float length) => Mathf.Repeat(distance, length * 4) / (length * 4) * Mathf.PI * 2;

    public void BuildSection(int section, float length, out Mesh ground, out Mesh village)
    {
        ground = Ground(section, length);
        var b = new Builder();
        var rng = new System.Random(unchecked(seed * 31 + section * 7499));
        float Range(float a, float c) => Mathf.Lerp(a, c, (float)rng.NextDouble());
        float LaneAt(float y) => Lane(Turn(section * length + y, length));

        // The main lane threads through asymmetrical hamlets, gardens, and open meadows.
        int side = (section + rng.Next(2)) % 2 == 0 ? -1 : 1;
        for (int i = 0; i < 5; i++)
        {
            float y = (i - 2) * 3.4f + Range(-.45f, .45f);
            int flank = i == 2 || i == 3 ? -side : side;
            float lane = LaneAt(y);
            float x = Mathf.Clamp(lane + flank * Range(2.15f, 2.85f), -4.35f, 4.35f);
            float size = i == 2 ? Range(1.25f, 1.4f) : Range(.86f, 1.13f);
            float angle = Range(-14, 15);
            bool chocolate = (section + i) % 3 == 0;
            b.Pose(new Vector2(x, y), angle, size);
            House(b, chocolate, i == 2, Gumdrops[(i + section) % Gumdrops.Length]);

            // Short iced paths connect front steps to the main lane.
            b.Pose(Vector2.zero);
            float reach = (chocolate ? .79f : .68f) * size;
            var front = new Vector2(x + Mathf.Sin(angle * Mathf.Deg2Rad) * reach,
                y - Mathf.Cos(angle * Mathf.Deg2Rad) * reach);
            var bend = new Vector2(Mathf.Lerp(x, lane, .57f), front.y - .34f);
            var road = new Vector2(LaneAt(bend.y) + flank * .37f, bend.y + .04f);
            b.Footpath(front, bend, road);

            // Candy hedges leave the footpath clear, with groups of tall striped canes behind.
            for (int j = 0; j < 4; j++)
            {
                float caneX = x + flank * (.82f + j * .49f);
                float caneY = y + Range(.61f, 1.24f) - .13f * j;
                b.Pose(new Vector2(caneX, caneY), Range(-26, 30), Range(.67f, .93f));
                Cane(b, j % 2 == 0 ? flank : -flank);
            }

            // Plant the candy gardens directly in the meadows.
            var garden = new Vector2(Mathf.Clamp(LaneAt(y + .25f) - flank * Range(2.4f, 3.6f), -4.5f, 4.5f), y + .25f);
            for (int j = 0; j < 3; j++)
            {
                b.Pose(garden + new Vector2((j - 1) * .48f, (j % 2) * .50f - .07f),
                    Range(-20, 20), Range(.58f, .84f));
                if (j == 1 && i % 2 == 0)
                    Lollipop(b);
                else
                    Gumdrop(b, Gumdrops[(i + j + 1) % Gumdrops.Length]);
            }
            for (int j = 0; j < 6; j++)
            {
                float berryX = x + flank * (.62f + j * .22f);
                b.Pose(new Vector2(berryX, y - .79f + Range(-.17f, .18f)), 0, Range(.75f, 1.24f));
                Gumdrop(b, Gumdrops[(j + i + section) % Gumdrops.Length]);
            }
        }

        for (int i = 0; i < 16; i++)
        {
            float y = Range(-8.65f, 8.1f);
            float offset = Range(4.15f, 11f);
            float x = LaneAt(y) + (i % 2 == 0 ? -offset : offset);
            b.Pose(new Vector2(x, y), Range(-33, 33), Range(.63f, 1.15f));
            if (i % 3 == 0)
                Lollipop(b);
            else
                Gumdrop(b, Gumdrops[rng.Next(Gumdrops.Length)]);
        }

        village = b.Mesh(VillageMeshName);
    }

    Mesh Ground(int section, float length)
    {
        // Every sample (including the winding lane and its border) lands at the same
        // position and colour on both sides of a join, including the wrap to section zero.
        const int rows = 80, columns = 64;
        const int stride = columns + 7;
        var points = new Vector3[(rows + 1) * stride];
        var colors = new Color[points.Length];
        var triangles = new int[rows * (stride - 1) * 6];
        for (int row = 0; row <= rows; row++)
        {
            float y = (row / (float)rows - .5f) * length;
            float turn = Turn(section * length + y, length);
            float lane = Lane(turn), width = LaneWidth(turn);
            float left = lane - width, right = lane + width;
            for (int col = 0; col < stride; col++)
            {
                int rim = col - columns / 2;
                float x;
                Color color;
                if (rim >= 0 && rim < 7)
                {
                    x = Mathf.Lerp(left - .16f, right + .16f, rim / 6f);
                    float paver = Mathf.Cos(turn * 56 + rim * Mathf.PI) * .018f;
                    color = rim == 0 || rim == 6 ? Color.Lerp(Rose, SugarLane, .43f)
                        : new Color(SugarLane.r + paver, SugarLane.g + paver, SugarLane.b + paver);
                }
                else
                {
                    x = rim < 0
                        ? Mathf.Lerp(-HalfWidth, left - .17f, col / (columns / 2f))
                        : Mathf.Lerp(right + .17f, HalfWidth, (rim - 6f) / (columns / 2f));
                    float ripple = Mathf.Sin(x * 1.25f + Mathf.Sin(turn * 7 + phase)) * .075f
                        + Mathf.Cos(x * 2.17f - turn * 13) * .034f
                        + Mathf.Sin(x * .58f + turn * 9) * .081f;
                    color = Color.Lerp(Rose, Strawberry, Mathf.Clamp01(.72f + ripple * 2.1f));
                }
                int index = row * stride + col;
                points[index] = new Vector3(x, y, 0);
                colors[index] = Distant(color);
                if (row == rows || col == stride - 1)
                    continue;
                int t = (row * (stride - 1) + col) * 6;
                triangles[t] = index;
                triangles[t + 1] = index + stride;
                triangles[t + 2] = index + 1;
                triangles[t + 3] = index + 1;
                triangles[t + 4] = index + stride;
                triangles[t + 5] = index + stride + 1;
            }
        }

        return new Mesh { name = "Strawberry fields and winding sugar lane", vertices = points, colors = colors, triangles = triangles };
    }

    static void House(Builder b, bool chocolate, bool large, Color sweet)
    {
        float w = large ? .90f : .73f, d = chocolate ? .63f : .52f;
        float eave = chocolate ? .69f : .78f, peak = eave + (chocolate ? .43f : .67f);
        Color wall = chocolate ? Cocoa : Biscuit;
        b.Shadow(.21f, -.17f, w + .54f, d + .34f);
        b.Box(new Vector3(0, 0, .065f), new Vector3(2 * w + .15f, 2 * d + .12f, .13f), MilkChocolate);
        b.Box(new Vector3(0, 0, eave * .5f), new Vector3(2 * w, 2 * d, eave), wall);
        b.Box(new Vector3(0, -d - .11f, .051f), new Vector3(.53f, .26f, .10f), Biscuit);

        for (int end = -1; end <= 1; end += 2)
            b.Face(Shade(wall, end < 0 ? .95f : .78f), new Vector3(-w, end * d, eave),
                new Vector3(w, end * d, eave), new Vector3(0, end * d, peak));

        // Each roof is built from two sloped surfaces and raised confectionery tiles.
        for (int side = -1; side <= 1; side += 2)
        {
            float roofX = side * (w + .13f), roofY = d + .12f;
            Color glaze = Shade(chocolate ? MilkChocolate : sweet, side < 0 ? 1f : .76f);
            Vector3 Roof(float across, float y, float lift = 0) =>
                new Vector3(roofX * across, y, Mathf.Lerp(peak + .05f, eave - .015f, across) + lift);

            b.Face(glaze, Roof(0, -roofY), Roof(1, -roofY), Roof(1, roofY), Roof(0, roofY));
            for (int row = 0; row < 4; row++)
            {
                float a = row * .25f + .024f, c = a + .206f;
                for (int tile = 0; tile < 5; tile++)
                {
                    float y0 = (tile / 5f * 2 - 1) * roofY + .016f;
                    float y1 = y0 + .4f * roofY - .037f;
                    b.Face(Shade(glaze, .77f), Roof(c, y0), Roof(c, y1), Roof(c, y1, .044f), Roof(c, y0, .044f));
                    b.Face(Shade(glaze, 1.06f + (row + tile) % 3 * .075f),
                        Roof(a, y0, .044f), Roof(c, y0, .044f), Roof(c, y1, .044f), Roof(a, y1, .044f));
                }
            }
            b.Pipe(new[] { Roof(0, -roofY, .05f), Roof(1, -roofY, .04f), Roof(1, roofY, .04f) }, .058f, Icing);
            for (int i = 0; i < 6; i++)
            {
                float y = Mathf.Lerp(-roofY, roofY, (i + .46f) / 6);
                b.Ball(Roof(1, y, -.04f), new Vector3(.075f, .07f, .10f + .032f * (i % 2)), Icing);
            }
        }

        b.Pipe(new[] { new Vector3(0, -d - .13f, peak + .10f), new Vector3(0, d + .13f, peak + .10f) }, .064f, Icing);
        b.Box(new Vector3(-w * .48f, d * .39f, peak - .02f), new Vector3(.24f, .23f, .52f), wall);
        b.Box(new Vector3(-w * .48f, d * .39f, peak + .26f), new Vector3(.30f, .28f, .10f), Icing);

        float front = -d - .017f;
        b.Disc(new Vector3(0, front, .51f), .153f, Icing, 16);
        b.Disc(new Vector3(0, front - .012f, .51f), .112f, BerryGlass, 16);
        b.Box(new Vector3(0, front - .022f, .51f), new Vector3(.193f, .025f, .028f), sweet);
        b.Box(new Vector3(0, front - .022f, .51f), new Vector3(.028f, .025f, .193f), sweet);
        Door(b, front, sweet);
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * w * .63f;
            b.Box(new Vector3(x, front, .35f), new Vector3(.25f, .05f, .25f), Icing);
            b.Box(new Vector3(x, front - .033f, .35f), new Vector3(.173f, .031f, .167f), BerryGlass);
            b.Box(new Vector3(x, front - .054f, .35f), new Vector3(.034f, .024f, .172f), sweet);
            b.Ball(new Vector3(x, front - .042f, .19f), new Vector3(.14f, .067f, .043f), sweet);
            b.Pipe(new[] { new Vector3(side * (w - .012f), front, .12f),
                new Vector3(side * (w - .012f), front, eave - .08f) }, .038f, Icing);
        }
    }

    static void Door(Builder b, float front, Color color)
    {
        var arch = new Vector3[11];
        arch[0] = new Vector3(.19f, front - .04f, .09f);
        arch[10] = new Vector3(-.19f, front - .04f, .09f);
        for (int i = 0; i < 9; i++)
        {
            float a = i * Mathf.PI / 8;
            arch[i + 1] = new Vector3(.19f * Mathf.Cos(a), front - .04f, .25f + .127f * Mathf.Sin(a));
        }
        b.Face(Shade(color, .68f), arch);
        b.Pipe(arch, .031f, Icing);
        b.Ball(new Vector3(.082f, front - .077f, .22f), Vector3.one * .038f, Icing);
    }

    static void Cane(Builder b, int direction)
    {
        const float crown = 1.04f, bend = .24f;
        var arc = new Vector3[25];
        for (int i = 0; i < 14; i++)
            arc[i] = new Vector3(0, 0, i / 13f * crown);
        for (int i = 14; i < arc.Length; i++)
        {
            float a = (i - 13f) / 11 * Mathf.PI;
            arc[i] = new Vector3(direction * bend * (1 - Mathf.Cos(a)), 0, crown + bend * Mathf.Sin(a));
        }

        b.Shadow(.11f, -.08f, .22f, .13f);
        b.Pipe(arc, .101f, Icing, Peppermint);
        b.Ball(arc[arc.Length - 1], Vector3.one * .100f, Peppermint);
    }

    static void Gumdrop(Builder b, Color color)
    {
        b.Shadow(.055f, -.045f, .24f, .19f);
        b.Ball(new Vector3(0, 0, .14f), new Vector3(.20f, .16f, .18f), color);
        b.Ball(new Vector3(-.071f, -.064f, .257f), Vector3.one * .034f, Color.Lerp(color, Icing, .6f));
    }

    static void Lollipop(Builder b)
    {
        b.Shadow(.12f, -.09f, .35f, .22f);
        b.Pipe(new[] { Vector3.zero, new Vector3(0, 0, 1.12f) }, .045f, Icing);
        b.Ball(new Vector3(0, 0, 1.15f), new Vector3(.345f, .115f, .33f), Peppermint);
        var spiral = new Vector3[29];
        for (int i = 0; i < spiral.Length; i++)
        {
            float t = i / (spiral.Length - 1f), r = Mathf.Lerp(.021f, .296f, t);
            spiral[i] = new Vector3(r * Mathf.Cos(t * 13f), -.115f, 1.15f + r * Mathf.Sin(t * 13f));
        }
        b.Pipe(spiral, .029f, Icing);
    }

    sealed class Builder
    {
        readonly List<Vector3> points = new List<Vector3>();
        readonly List<Color> colors = new List<Color>();
        readonly List<int> indices = new List<int>();
        Vector2 origin;
        float sin, cos = 1, scale = 1;

        public void Pose(Vector2 at, float degrees = 0, float size = 1)
        {
            origin = at;
            sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
            cos = Mathf.Cos(degrees * Mathf.Deg2Rad);
            scale = size;
        }

        public void Face(Color color, params Vector3[] shape)
        {
            int first = points.Count;
            Color distant = Distant(color);
            foreach (var p in shape)
            {
                // The game looks down Z. Shear the model height up the screen to show
                // the facades while retaining depth for roofs, trim and overlapping props.
                points.Add(new Vector3(origin.x + (p.x * cos - p.y * sin + p.z * .28f) * scale,
                    origin.y + (p.x * sin + p.y * cos + p.z * .61f) * scale, -p.z * scale));
                colors.Add(distant);
            }
            for (int i = 1; i < shape.Length - 1; i++)
            {
                indices.Add(first);
                indices.Add(first + i);
                indices.Add(first + i + 1);
            }
        }

        public void Box(Vector3 center, Vector3 size, Color color)
        {
            Vector3 h = size * .5f;
            Vector3 P(float x, float y, float z) => center + new Vector3(x * h.x, y * h.y, z * h.z);
            Face(Shade(color, .81f), P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), P(-1, -1, 1));
            Face(Shade(color, .68f), P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), P(1, -1, 1));
            Face(Shade(color, .94f), P(-1, 1, -1), P(-1, -1, -1), P(-1, -1, 1), P(-1, 1, 1));
            Face(Shade(color, .71f), P(1, 1, -1), P(-1, 1, -1), P(-1, 1, 1), P(1, 1, 1));
            Face(color, P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1));
        }

        public void Disc(Vector3 center, float radius, Color color, int sides)
        {
            var ring = new Vector3[sides];
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides;
                ring[i] = center + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius;
            }
            Face(color, ring);
        }

        public void Footpath(Vector2 front, Vector2 bend, Vector2 road)
        {
            Vector2 a = Vector2.Perpendicular((bend - front).normalized);
            Vector2 b = Vector2.Perpendicular((road - front).normalized);
            Vector2 c = Vector2.Perpendicular((road - bend).normalized);
            Vector3 P(Vector2 at) => new Vector3(at.x, at.y, .012f);
            Face(SugarLane, P(front - a * .12f), P(bend - b * .155f), P(bend + b * .155f), P(front + a * .12f));
            Face(SugarLane, P(bend - b * .155f), P(road - c * .21f), P(road + c * .21f), P(bend + b * .155f));
        }

        public void Shadow(float x, float y, float width, float depth)
        {
            for (int ring = 0; ring < 3; ring++)
            {
                float factor = 1 - ring * .16f;
                var shape = new Vector3[14];
                for (int i = 0; i < shape.Length; i++)
                {
                    float a = i * Mathf.PI * 2 / shape.Length;
                    shape[i] = new Vector3(x + Mathf.Cos(a) * width * factor,
                        y + Mathf.Sin(a) * depth * factor, .002f * (ring + 1));
                }
                Face(new Color(.27f, .062f, .16f, .13f + ring * .035f), shape);
            }
        }

        public void Ball(Vector3 center, Vector3 radii, Color color)
        {
            const int rings = 5, sides = 10;
            Vector3 Unit(int ring, int i)
            {
                float v = Mathf.PI * ring / rings, a = Mathf.PI * 2 * i / sides;
                return new Vector3(Mathf.Cos(a) * Mathf.Sin(v), Mathf.Sin(a) * Mathf.Sin(v), Mathf.Cos(v));
            }
            Vector3 Point(Vector3 unit) => center + Vector3.Scale(unit, radii);
            Vector3 light = new Vector3(-.44f, -.49f, .74f);
            for (int ring = 0; ring < rings; ring++)
                for (int i = 0; i < sides; i++)
                {
                    Vector3 a = Unit(ring, i), b = Unit(ring, i + 1);
                    Vector3 c = Unit(ring + 1, i + 1), d = Unit(ring + 1, i);
                    float lit = Mathf.Max(0, Vector3.Dot((a + b + c + d).normalized, light));
                    Face(Shade(color, .60f + .43f * lit + .19f * lit * lit), Point(a), Point(b), Point(c), Point(d));
                }
        }

        public void Pipe(Vector3[] path, float radius, Color main, Color? stripe = null)
        {
            const int sides = 8;
            Vector3 Ring(int point, int side)
            {
                var tangent = (path[Mathf.Min(path.Length - 1, point + 1)] - path[Mathf.Max(0, point - 1)]).normalized;
                var axis = Mathf.Abs(tangent.y) < .94f ? Vector3.up : Vector3.right;
                var right = Vector3.Cross(tangent, axis).normalized;
                var forward = Vector3.Cross(right, tangent);
                float a = side * Mathf.PI * 2 / sides;
                return path[point] + (right * Mathf.Cos(a) + forward * Mathf.Sin(a)) * radius;
            }

            for (int i = 0; i < path.Length - 1; i++)
                for (int j = 0; j < sides; j++)
                {
                    Color color = stripe.HasValue && (i * 2 + j) % 8 < 4 ? stripe.Value : main;
                    float light = .80f + .18f * Mathf.Cos((j - 1) * Mathf.PI * 2 / sides);
                    Face(Shade(color, light), Ring(i, j), Ring(i, j + 1), Ring(i + 1, j + 1), Ring(i + 1, j));
                }
        }

        public Mesh Mesh(string name)
        {
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(points);
            mesh.SetColors(colors);
            mesh.SetTriangles(indices, 0);
            return mesh;
        }
    }
}

public static partial class IndustrialMeshFactory
{
    static readonly Mesh[] candyProps = new Mesh[5];
    public static Mesh CandyProp(int kind)
    {
        if (candyProps[kind] != null)
            return candyProps[kind];
        var authored = TerraModel("PropCandy" + kind);
        if (authored != null)
            return candyProps[kind] = authored;
        var b = new Builder();
        if (kind == 0)
        {
            b.Box(new Vector3(0, -.95f, .09f), new Vector3(.13f, 1.8f, .13f), 15, 1);
            b.Drum(new Vector3(0, .18f, 0), .69f, .22f, 15, 20, 8);
            b.Drum(new Vector3(0, .18f, .04f), .72f, .14f, 8, 20, 0);
            // A real raised spiral winds across the confection's face.
            for (int i = 0; i < 48; i++)
            {
                float a = i / 48f * Mathf.PI * 4.8f, c = (i + 1) / 48f * Mathf.PI * 4.8f, r = .055f + i / 48f * .55f, t = .055f + (i + 1) / 48f * .55f;
                Vector3 P(float angle, float radius) => new Vector3(Mathf.Cos(angle) * radius, .18f + Mathf.Sin(angle) * radius, -.12f);
                b.Face(0, P(c, t - .034f), P(c, t + .034f), P(a, r + .034f), P(a, r - .034f));
            }
        }
        else if (kind == 1)
        {
            b.Box(Vector3.zero, new Vector3(1.08f, 1.65f, .20f), 4, 5);
            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 2; x++)
                    b.Prism(new[] { V(-.19f, -.20f), V(.19f, -.20f), V(.23f, -.16f), V(.23f, .16f), V(.19f, .20f), V(-.19f, .20f), V(-.23f, .16f), V(-.23f, -.16f) }, .12f, 5, new Vector3(-.26f + x * .52f, -.52f + y * .52f, -.15f), 4);
            b.Box(new Vector3(0, -.56f, -.23f), new Vector3(1.14f, .57f, .045f), 7, 14);
            b.Box(new Vector3(0, -.32f, -.26f), new Vector3(1.14f, .08f, .03f), 10, 1);
            b.Prism(new[] { V(-.54f, 0), V(-.29f, .16f), V(-.04f, -.02f), V(.20f, .13f), V(.54f, 0) }, .035f, 15, new Vector3(0, -.28f, -.27f), 1);
            b.Prism(new[] { V(0, -.12f), V(-.12f, 0), V(0, .12f), V(.12f, 0) }, .035f, 10, new Vector3(0, -.58f, -.28f), 1);
        }
        else if (kind == 2)
        {
            b.Ball(Vector3.zero, new Vector3(.53f, .42f, .27f), 2);
            foreach (int s in new[] { -1, 1 })
                b.Prism(new[] { V(s * .44f, 0), V(s * .91f, -.32f), V(s * .82f, 0), V(s * .91f, .32f) }, .12f, 15, Vector3.zero, 2);
        }
        else if (kind == 3)
        {
            b.Drum(Vector3.zero, .59f, .18f, 1, 16, 1);
            b.Ball(new Vector3(0, 0, -.16f), new Vector3(.64f, .64f, .23f), 8);
            b.Ball(new Vector3(0, 0, .16f), new Vector3(.64f, .64f, .23f), 0);
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI / 8;
                var p = new Vector3(Mathf.Cos(a) * .56f, Mathf.Sin(a) * .56f, -.1f);
                b.Ball(p, new Vector3(.10f, .10f, .09f), 8);
            }
        }
        else
        {
            // An eight-sided sugar tube bends into a real hooked cane.
            var points = new Vector3[25];
            for (int i = 0; i < 13; i++)
                points[i] = new Vector3(.36f, -1.05f + i * .12f, 0);
            for (int i = 13; i < 25; i++)
            {
                float a = (i - 12) / 12f * Mathf.PI;
                points[i] = new Vector3(Mathf.Cos(a) * .36f, .39f + Mathf.Sin(a) * .36f, 0);
            }
            for (int i = 0; i < 24; i++)
            {
                var tangent = (points[i + 1] - points[i]).normalized;
                var right = new Vector3(-tangent.y, tangent.x, 0);
                Vector3 P(int k, bool end)
                {
                    float a = k * Mathf.PI / 4;
                    return points[end ? i + 1 : i] + (right * Mathf.Cos(a) + Vector3.forward * Mathf.Sin(a)) * .12f;
                }
                for (int j = 0; j < 8; j++)
                    b.Face(i % 4 < 2 ? 15 : 0, P(j, false), P(j + 1, false), P(j + 1, true), P(j, true));
            }
            b.Ball(points[0], Vector3.one * .12f, 15);
            b.Ball(points[24], Vector3.one * .12f, 0);
        }
        return candyProps[kind] = b.Finish(new[] { "Spiral lollipop", "Foil wrapped chocolate bar", "Mint wrapper candy", "Strawberry cream macaron", "Striped hooked candy cane" }[kind]);
    }
}
