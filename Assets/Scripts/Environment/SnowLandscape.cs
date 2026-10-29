using System.Collections.Generic;
using UnityEngine;

// One periodic plan for groves, glades, ice hollows and their ground contact.
public sealed class SnowLandscape
{
    public const float Period = 80;
    public readonly List<PSXValleyLayout.Plant> Plants = new List<PSXValleyLayout.Plant>();
    public readonly List<PSXValleyLayout.Plant> NearPlants = new List<PSXValleyLayout.Plant>();
    public readonly Vector2[] Pools = new Vector2[3];
    public readonly Vector3[] PoolShapes = new Vector3[3];
    public readonly int Seed;
    readonly float phase, phase2;
    public SnowLandscape(int seed)
    {
        Seed = seed;
        var rng = new System.Random(seed ^ 0x57a91);
        phase = Range(rng, 0, Mathf.PI * 2);
        phase2 = Range(rng, 0, Mathf.PI * 2);
        for (int i = 0; i < Pools.Length; i++)
        {
            Pools[i] = new Vector2((i % 2 == 0 ? -1 : 1) * Range(rng, 1.9f, 3.1f), i * 26 + Range(rng, -4, 4));
            PoolShapes[i] = new Vector3(Range(rng, 1.05f, 1.55f), Range(rng, 1.4f, 2.25f), Range(rng, -.65f, .65f));
        }
        // Asymmetric pockets of woodland alternate with broad, quiet combat clearings.
        for (int grove = 0; grove < 33; grove++)
        {
            float y = Wrap(-9 + grove * Period / 33 + Range(rng, -1.6f, 1.6f));
            float side = rng.Next(2) == 0 ? -1 : 1, x = side * Range(rng, 3.5f, 7.4f);
            int count = rng.Next(3, 7), kind = grove % 5 == 0 ? 1 : grove % 3 == 0 ? 3 : 0;
            for (int member = 0; member < count; member++)
            {
                float angle = Range(rng, 0, Mathf.PI * 2), reach = Range(rng, .1f, 1.5f);
                float tx = x + Mathf.Cos(angle) * reach, ty = Wrap(y + Mathf.Sin(angle) * reach);
                float radius = Range(rng, .44f, kind == 1 ? .72f : .93f);
                if (!Clear(tx, ty, radius))
                    continue;
                bool room = true;
                foreach (var other in Plants)
                    if (new Vector2(tx - other.x, DistanceY(ty, other.y)).sqrMagnitude < Mathf.Pow((radius + other.radius) * .77f, 2))
                    {
                        room = false;
                        break;
                    }
                if (room)
                    Plants.Add(new PSXValleyLayout.Plant { x = tx, y = ty, radius = radius, kind = kind, angle = Range(rng, 0, 360), seed = rng.Next() });
            }
        }
        for (int i = 0; i < 60; i++)
        {
            float x = (i % 2 == 0 ? -1 : 1) * Range(rng, 2.9f, 7.4f), y = Range(rng, -10, 70);
            if (Clear(x, y, .2f))
                Plants.Add(new PSXValleyLayout.Plant { x = x, y = y, radius = Range(rng, .12f, .23f), kind = 2, angle = Range(rng, 0, 360), seed = rng.Next() });
        }
        for (int i = 0; i < 24; i++)
            NearPlants.Add(new PSXValleyLayout.Plant
            {
                x = (i % 2 == 0 ? -1 : 1) * Range(rng, 7.3f, 10.3f),
                y = Range(rng, -10, 70),
                radius = Range(rng, .8f, 1.3f),
                kind = i % 4 == 0 ? 1 : 0,
                angle = Range(rng, 0, 360),
                seed = rng.Next()
            });
    }

    public bool Clear(float x, float y, float radius)
    {
        if (Mathf.Abs(x) < 2.45f + radius * .35f || Mathf.Abs(x) > 8.9f)
            return false;
        for (int i = 0; i < Pools.Length; i++)
        {
            var pool = Pools[i];
            var shape = PoolShapes[i];
            float dy = SignedDistanceY(y, pool.y), dx = x - pool.x, cs = Mathf.Cos(shape.z), sn = Mathf.Sin(shape.z);
            float px = (dx * cs + dy * sn) / (shape.x * 1.12f + radius), py = (-dx * sn + dy * cs) / (shape.y * 1.12f + radius);
            if (px * px + py * py < 1)
                return false;
        }
        return true;
    }

    public float Height(float x, float y)
    {
        float t = y * Mathf.PI * 2 / Period;
        float drift = .15f * Mathf.Sin(t * 3 + x * .42f + phase) + .075f * Mathf.Cos(t * 7 - x * .7f + phase2);
        float shoulder = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(3, 9, Mathf.Abs(x))) * .34f;
        return .38f + drift + shoulder;
    }

    public Color SnowColor(float x, float y)
    {
        float dx = (Height(x + .12f, y) - Height(x - .12f, y)) / .24f, dy = (Height(x, y + .12f) - Height(x, y - .12f)) / .24f;
        float light = Mathf.Clamp01(.57f - dx * 1.8f + dy * 3.2f);
        var color = Color.Lerp(new Color(.60f, .72f, .79f), new Color(.88f, .92f, .91f), light);
        float pocket = .5f + .5f * Mathf.Sin(y * Mathf.PI * 4 / Period + x * .55f + phase2);
        return Color.Lerp(color, new Color(.73f, .82f, .86f), pocket * .16f);
    }

    public static float Wrap(float y) => Mathf.Repeat(y + 10, Period) - 10;
    public static float DistanceY(float a, float b) => Mathf.Abs(Mathf.Repeat(a - b + Period / 2, Period) - Period / 2);
    static float SignedDistanceY(float a, float b) => Mathf.Repeat(a - b + Period / 2, Period) - Period / 2;
    public static float Range(System.Random rng, float lo, float hi) => Mathf.Lerp(lo, hi, (float)rng.NextDouble());
}

public static class SnowLandscapeGeometry
{
    public static Mesh Ground(int section, SnowLandscape layout)
    {
        var b = new Builder(section * 20);
        // Keep the exact 40x16 quad order used by the augers' surface projection.
        for (int row = 0; row < 40; row++)
            for (int col = 0; col < 16; col++)
            {
                float x = -11 + col * 22f / 16, y = section * 20 - 10 + row * .5f;
                b.Surface(layout, x, y, x + 22f / 16, y + .5f);
            }
        return b.Finish("Sculpted snowfield " + section);
    }

    public static Mesh Detail(int section, SnowLandscape layout, bool near = false)
    {
        float origin = section * 20;
        var b = new Builder(origin);
        var rng = new System.Random(unchecked(layout.Seed * 397 + section * 7919));
        var plants = near ? layout.NearPlants : layout.Plants;
        foreach (var plant in plants)
        {
            if (plant.kind == 2 || plant.y < origin - 10 || plant.y >= origin + 10)
                continue;
            b.Tree(plant, layout);
        }
        if (!near)
        {
            for (int i = 0; i < layout.Pools.Length; i++)
            {
                var pool = layout.Pools[i];
                if (pool.y >= origin - 10 && pool.y < origin + 10)
                    b.Pool(pool, layout.PoolShapes[i], layout, new System.Random(layout.Seed + (int)(pool.y * 713)));
            }
            // Bedrock appears in composed clusters, leaving open powder between them.
            for (int cluster = 0; cluster < 5; cluster++)
            {
                float y = origin - 8 + cluster * 3.8f + SnowLandscape.Range(rng, -.7f, .7f);
                float x = (cluster % 2 == section % 2 ? -1 : 1) * SnowLandscape.Range(rng, 3.5f, 6.6f);
                for (int stone = 0; stone < 3; stone++)
                {
                    float sx = x + SnowLandscape.Range(rng, -.65f, .65f), sy = y + SnowLandscape.Range(rng, -.45f, .45f), r = SnowLandscape.Range(rng, .21f, .57f);
                    if (layout.Clear(sx, sy, r))
                        b.Rock(sx, sy, r, layout, rng);
                }
                if (cluster == 2 && layout.Clear(x, y, .8f))
                    b.FallenLog(x, y, layout, rng);
            }
        }
        return b.Finish((near ? "Near winter trunks " : "Ice hollows, winter trunks and snow-capped rock ") + section);
    }

    sealed class Builder
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<Color> colors = new List<Color>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<int> triangles = new List<int>();
        readonly float origin;
        public Builder(float origin)
        {
            this.origin = origin;
        }

        Vector3 P(float x, float y, float height) => new Vector3(x, y - origin, -height);
        void Face(Color color, params Vector3[] points)
        {
            int first = vertices.Count;
            color.a = 1;
            foreach (var p in points)
            {
                vertices.Add(P(p.x, p.y, p.z));
                colors.Add(color);
                uv.Add(new Vector2(p.x, p.y));
            }
            for (int i = 1; i < points.Length - 1; i++)
            {
                triangles.Add(first);
                triangles.Add(first + i);
                triangles.Add(first + i + 1);
            }
        }

        public void Surface(SnowLandscape layout, float x, float y, float right, float top)
        {
            int first = vertices.Count;
            foreach (var p in new[] { new Vector2(x, y), new Vector2(right, y), new Vector2(right, top), new Vector2(x, top) })
            {
                vertices.Add(P(p.x, p.y, layout.Height(p.x, p.y)));
                colors.Add(layout.SnowColor(p.x, p.y));
                uv.Add(p);
            }
            triangles.AddRange(new[] { first, first + 1, first + 2, first, first + 2, first + 3 });
        }

        void GradientTriangle(Vector3 a, Vector3 b, Vector3 c, Color edge, Color centre)
        {
            int first = vertices.Count;
            foreach (var p in new[] { a, b, c })
            {
                vertices.Add(P(p.x, p.y, p.z));
                uv.Add(new Vector2(p.x, p.y));
            }
            colors.Add(edge);
            colors.Add(edge);
            colors.Add(centre);
            triangles.AddRange(new[] { first, first + 1, first + 2 });
        }

        void Beam(Vector3 a, Vector3 b, float width, Color color)
        {
            var d = (b - a).normalized;
            var side = Vector3.Cross(d, Vector3.forward).normalized * width * .5f;
            if (side.sqrMagnitude < .000001f)
                side = Vector3.right * width * .5f;
            var depth = Vector3.Cross(d, side).normalized * width * .45f;
            var lo = new[] { a - side - depth, a + side - depth, a + side + depth, a - side + depth };
            var hi = new[] { b - side - depth, b + side - depth, b + side + depth, b - side + depth };
            for (int i = 0; i < 4; i++)
                Face(color * (i == 1 ? .76f : 1), lo[i], hi[i], hi[(i + 1) % 4], lo[(i + 1) % 4]);
            Face(color, hi);
        }

        void Patch(float x, float y, float rx, float ry, Color color, SnowLandscape layout, float lift = .012f)
        {
            var centre = new Vector3(x, y, layout.Height(x, y) + lift);
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 6, n = (i + 1) * Mathf.PI / 6;
                Vector3 Edge(float t)
                {
                    float px = x + Mathf.Cos(t) * rx, py = y + Mathf.Sin(t) * ry;
                    return new Vector3(px, py, layout.Height(px, py) + lift);
                }
                Face(color, centre, Edge(a), Edge(n));
            }
        }

        public void Tree(PSXValleyLayout.Plant plant, SnowLandscape layout)
        {
            float x = plant.x, y = plant.y, r = plant.radius, h = layout.Height(x, y);
            var rng = new System.Random(plant.seed);
            Patch(x + .18f, y - .19f, r * 1.17f, r * .73f, new Color(.59f, .70f, .75f), layout);
            float height = SnowLandscape.Range(rng, .95f, 1.3f);
            Vector3 root = new Vector3(x, y, h), fork = new Vector3(x + SnowLandscape.Range(rng, -.15f, .15f) * r, y + .19f, h + height);
            var bark = plant.kind == 1 ? new Color(.68f, .73f, .69f) : new Color(.30f, .34f, .30f);
            Beam(root, fork, r * .18f, bark);
            if (plant.kind == 1)
            {
                for (int band = 0; band < 5; band++)
                {
                    var at = Vector3.Lerp(root, fork, (band + 1) / 6f) + new Vector3(0, -r * .085f, 0);
                    float w = r * SnowLandscape.Range(rng, .045f, .09f);
                    Face(new Color(.30f, .36f, .35f), at + new Vector3(-w, 0, 0), at + new Vector3(w, 0, .018f), at + new Vector3(w * .6f, 0, .045f), at + new Vector3(-w, 0, .025f));
                }
            }
            if (plant.kind == 1)
            {
                for (int branch = 0; branch < 4; branch++)
                {
                    float angle = plant.angle * Mathf.Deg2Rad + branch * Mathf.PI * .5f + SnowLandscape.Range(rng, -.3f, .3f);
                    var start = Vector3.Lerp(root, fork, .48f + branch * .09f);
                    var tip = start + new Vector3(Mathf.Cos(angle) * r * .65f, Mathf.Sin(angle) * r * .52f, height * .24f);
                    Beam(start, tip, r * .055f, bark * .82f);
                }
            }
            else
            {
                // Uneven whorls support the existing textured evergreen crown cards.
                float rotation = plant.angle * Mathf.Deg2Rad;
                for (int tier = 0; tier < 3; tier++)
                {
                    var hub = Vector3.Lerp(root, fork, .42f + tier * .19f);
                    float reach = r * (1.03f - tier * .22f) * SnowLandscape.Range(rng, .88f, 1.08f);
                    for (int branch = 0; branch < 5; branch++)
                    {
                        float angle = rotation + branch * Mathf.PI * 2 / 5 + tier * .38f;
                        float next = rotation + (branch + 1) * Mathf.PI * 2 / 5 + tier * .38f;
                        var tip = hub + new Vector3(Mathf.Cos(angle) * reach, Mathf.Sin(angle) * reach * .8f, -height * .10f);
                        var nextTip = hub + new Vector3(Mathf.Cos(next) * reach, Mathf.Sin(next) * reach * .8f, -height * .10f);
                        var peak = hub + Vector3.forward * (height * .13f);
                        Face(new Color(.24f, .36f, .31f) * (branch % 2 == 0 ? 1f : .85f), tip, nextTip, peak);
                        var snowA = Vector3.Lerp(tip, peak, .30f) + Vector3.forward * .008f;
                        var snowB = Vector3.Lerp(nextTip, peak, .30f) + Vector3.forward * .008f;
                        Face(new Color(.78f, .86f, .85f), snowA, snowB, peak + Vector3.forward * .008f);
                    }
                }
            }
        }

        public void Rock(float x, float y, float radius, SnowLandscape layout, System.Random rng)
        {
            float floor = layout.Height(x, y);
            Patch(x + .12f, y - .12f, radius * 1.25f, radius * .8f, new Color(.61f, .71f, .75f), layout);
            float stretch = SnowLandscape.Range(rng, .72f, 1f);
            float rotation = SnowLandscape.Range(rng, 0, Mathf.PI * 2);
            var top = new Vector3(x + radius * SnowLandscape.Range(rng, -.25f, .25f), y + radius * SnowLandscape.Range(rng, -.18f, .18f), floor + radius * SnowLandscape.Range(rng, .45f, .85f));
            var foot = new Vector3[7];
            var shoulder = new Vector3[7];
            for (int i = 0; i < 7; i++)
            {
                float angle = rotation + i * Mathf.PI * 2 / 7;
                float reach = radius * SnowLandscape.Range(rng, .82f, 1.12f);
                float px = x + Mathf.Cos(angle) * reach, py = y + Mathf.Sin(angle) * reach * stretch;
                foot[i] = new Vector3(px, py, layout.Height(px, py) + .025f);
                shoulder[i] = Vector3.Lerp(foot[i], top, SnowLandscape.Range(rng, .32f, .48f));
            }
            for (int i = 0; i < 7; i++)
            {
                int next = (i + 1) % 7;
                var stone = Color.Lerp(new Color(.35f, .43f, .46f), new Color(.56f, .64f, .64f), i / 7f);
                Face(stone * .85f, foot[i], foot[next], shoulder[next], shoulder[i]);
                Face(stone, shoulder[i], shoulder[next], top);
                var u = Vector3.Lerp(top, shoulder[i], .85f) + Vector3.forward * .016f;
                var v = Vector3.Lerp(top, shoulder[next], .85f) + Vector3.forward * .016f;
                Face(new Color(.83f, .89f, .90f) * (i % 2 == 0 ? .97f : .90f), u, v, top + Vector3.forward * .016f);
            }
        }

        public void FallenLog(float x, float y, SnowLandscape layout, System.Random rng)
        {
            float angle = SnowLandscape.Range(rng, -Mathf.PI, Mathf.PI);
            float length = SnowLandscape.Range(rng, 1.1f, 1.65f);
            float width = SnowLandscape.Range(rng, .19f, .29f);
            var axis = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var side = new Vector3(-axis.y, axis.x, 0);
            float ax = x - axis.x * length * .5f, ay = y - axis.y * length * .5f;
            float bx = x + axis.x * length * .5f, by = y + axis.y * length * .5f;
            Vector3 a = new Vector3(ax, ay, layout.Height(ax, ay) + width * .48f);
            Vector3 b = new Vector3(bx, by, layout.Height(bx, by) + width * .48f);
            Beam(a, b, width, new Color(.36f, .35f, .29f));
            var snowA = a + Vector3.forward * width * .48f;
            var snowB = b + Vector3.forward * width * .48f;
            Face(new Color(.85f, .90f, .88f), snowA - side * width * .38f, snowB - side * width * .38f, snowB + side * width * .38f, snowA + side * width * .38f);
            var branch = Vector3.Lerp(a, b, SnowLandscape.Range(rng, .3f, .65f));
            Beam(branch, branch + side * SnowLandscape.Range(rng, .24f, .4f) + Vector3.forward * .18f, width * .3f, new Color(.39f, .40f, .33f));
        }

        public void Pool(Vector2 pool, Vector3 shape, SnowLandscape layout, System.Random rng)
        {
            const int sides = 16;
            var rim = new Vector3[sides];
            var inside = new Vector3[sides];
            float phase = SnowLandscape.Range(rng, 0, Mathf.PI * 2);
            float cs = Mathf.Cos(shape.z), sn = Mathf.Sin(shape.z);
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides, r = 1 + .07f * Mathf.Sin(a * 3 + phase) + .04f * Mathf.Cos(a * 5 - phase);
                float px = Mathf.Cos(a) * shape.x * r, py = Mathf.Sin(a) * shape.y * r;
                float x = pool.x + px * cs - py * sn, y = pool.y + px * sn + py * cs;
                rim[i] = new Vector3(x, y, layout.Height(x, y) + .065f);
                float ix = Mathf.Lerp(pool.x, x, .82f), iy = Mathf.Lerp(pool.y, y, .82f);
                inside[i] = new Vector3(ix, iy, layout.Height(ix, iy) + .025f);
            }
            var centre = new Vector3(pool.x, pool.y, layout.Height(pool.x, pool.y) + .03f);
            for (int i = 0; i < sides; i++)
            {
                int n = (i + 1) % sides;
                Face(i % 3 == 0 ? new Color(.75f, .84f, .86f) : new Color(.81f, .88f, .89f), rim[i], rim[n], inside[n], inside[i]);
                GradientTriangle(inside[i], inside[n], centre, new Color(.64f, .79f, .82f), new Color(.47f, .67f, .74f));
            }
            // Fine opaque scratches remain attached to the ice rather than flashing like bullets.
            for (int crack = 0; crack < 4; crack++)
            {
                float u = SnowLandscape.Range(rng, -.35f, .35f), v = SnowLandscape.Range(rng, -.35f, .35f);
                Vector3 IcePoint(float du, float dv)
                {
                    float px = (u + du) * shape.x, py = (v + dv) * shape.y;
                    float x = pool.x + px * cs - py * sn, y = pool.y + px * sn + py * cs;
                    return new Vector3(x, y, layout.Height(x, y) + .05f);
                }
                Vector3 start = IcePoint(-.12f, -.12f), joint = IcePoint(.02f, .02f);
                Beam(start, joint, .015f, new Color(.76f, .86f, .87f));
                Beam(joint, IcePoint(-.04f, .17f), .012f, new Color(.76f, .86f, .87f));
            }
        }

        public Mesh Finish(string name)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
