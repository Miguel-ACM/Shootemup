using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// City as a destroyed city (Miguel, 15 November 2026: the tidy street grid looked awful). No grid
// anywhere: one winding, cracked road runs through ash, broken concrete, scorch marks, rubble
// fields and craters; ruins stand at their own angles: gutted blocks with jagged walls, exposed
// floor slabs and dark window holes, bare tower skeletons, collapsed mounds, a toppled tower and
// broken overpass spans. Burnt-out cars, tank traps and small fires fill the gaps. Everything is
// seeded by the run and laid out in loop space, so the scroll stays seamless.
public static class CityRuinsGeometry
{
    // The same fake three-quarter view as the rest of the scenery: height leans up and right.
    const float LeanX = .14f, LeanY = .24f;
    static readonly Vector2 ShadowPerHeight = new Vector2(.38f, -.26f);
    const float ShadowPlane = .03f, SectionLength = 20, LoopLength = 80, Half = 10.8f;
    static readonly Color ShadowColor = new Color(.02f, .022f, .03f, .44f);
    static readonly Vector2 Sun = new Vector2(-.45f, .65f).normalized;

    static Color C(float r, float g, float b) => new Color(r, g, b, 1);
    static readonly Color Ash = C(.25f, .24f, .23f), Scorch = C(.11f, .10f, .10f), Dirt = C(.30f, .25f, .19f), Slab = C(.40f, .39f, .37f);
    static readonly Color Asphalt = C(.16f, .16f, .17f), Crack = C(.07f, .07f, .075f), Marking = C(.55f, .50f, .34f);
    static readonly Color[] Masonry = { C(.44f, .43f, .41f), C(.36f, .35f, .34f), C(.47f, .29f, .23f), C(.52f, .46f, .36f), C(.33f, .36f, .37f) };
    static readonly Color Soot = C(.12f, .11f, .11f), Steel = C(.24f, .23f, .24f), Rust = C(.34f, .21f, .14f), Hole = C(.06f, .06f, .07f);
    static readonly Color Flame = C(1f, .55f, .16f), FlameCore = C(1f, .86f, .42f);

    sealed class Builder
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<Color> colors = new List<Color>();
        readonly List<int> triangles = new List<int>();
        readonly List<Vector3> shade = new List<Vector3>();
        readonly List<Color> shadeColors = new List<Color>();
        readonly List<int> shadeTriangles = new List<int>();

        static Vector3 Lean(Vector3 q) => new Vector3(q.x + q.z * LeanX, q.y + q.z * LeanY, -q.z);

        public void Face(Color c, params Vector3[] points)
        {
            int first = vertices.Count;
            foreach (var p in points)
            {
                vertices.Add(Lean(p));
                colors.Add(c);
            }
            for (int i = 1; i < points.Length - 1; i++)
            {
                triangles.Add(first);
                triangles.Add(first + i);
                triangles.Add(first + i + 1);
            }
        }

        // A flat decal on the ground at a small height (layering order without z-fighting).
        public void Flat(Color c, float z, params Vector2[] points)
        {
            var lifted = new Vector3[points.Length];
            for (int i = 0; i < points.Length; i++)
                lifted[i] = new Vector3(points[i].x, points[i].y, z);
            Face(c, lifted);
        }

        // A vertical prism over a convex footprint, sides shaded by how they face the sun.
        public void Prism(Vector2[] footprint, float bottom, float top, Color c, Color? roof = null)
        {
            if (top - bottom < .001f)
                return;
            int n = footprint.Length;
            for (int i = 0; i < n; i++)
            {
                var a = footprint[i];
                var b = footprint[(i + 1) % n];
                var edge = (b - a).normalized;
                var outward = new Vector2(edge.y, -edge.x);
                float light = .58f + .36f * Mathf.Max(0, Vector2.Dot(outward, Sun)) + .06f * Mathf.Max(0, -outward.y);
                Face(c * light, new Vector3(a.x, a.y, bottom), new Vector3(b.x, b.y, bottom), new Vector3(b.x, b.y, top), new Vector3(a.x, a.y, top));
            }
            var cap = new Vector3[n];
            for (int i = 0; i < n; i++)
                cap[i] = new Vector3(footprint[i].x, footprint[i].y, top);
            Face(roof ?? c, cap);
            if (bottom < .2f)
                Shadow(footprint, top);
        }

        public void Shadow(Vector2[] footprint, float top)
        {
            if (top < .12f)
                return;
            var points = new List<Vector2>(footprint.Length * 2);
            var shift = ShadowPerHeight * top;
            foreach (var p in footprint)
            {
                points.Add(p);
                points.Add(p + shift);
            }
            var hull = ConvexHull(points);
            int first = shade.Count;
            foreach (var p in hull)
            {
                shade.Add(Lean(new Vector3(p.x, p.y, ShadowPlane)));
                shadeColors.Add(ShadowColor);
            }
            for (int i = 1; i < hull.Count - 1; i++)
            {
                shadeTriangles.Add(first);
                shadeTriangles.Add(first + i);
                shadeTriangles.Add(first + i + 1);
            }
        }

        static List<Vector2> ConvexHull(List<Vector2> points)
        {
            points.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            var hull = new List<Vector2>();
            float Cross(Vector2 o, Vector2 a, Vector2 b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
            foreach (var p in points)
            {
                while (hull.Count >= 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], p) <= 0)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(p);
            }
            int lower = hull.Count + 1;
            for (int i = points.Count - 2; i >= 0; i--)
            {
                var p = points[i];
                while (hull.Count >= lower && Cross(hull[hull.Count - 2], hull[hull.Count - 1], p) <= 0)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(p);
            }
            hull.RemoveAt(hull.Count - 1);
            return hull;
        }

        public Mesh Finish(string name)
        {
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public Mesh FinishShadows(string name)
        {
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(shade);
            mesh.SetColors(shadeColors);
            mesh.SetTriangles(shadeTriangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    // Loop position of a section-local height.
    static float Loop(int section, float y) => (section - 1) * SectionLength + y;

    // The one road: a winding centreline with a period of the whole loop, so it joins itself.
    static float RoadX(int seed, float loopY)
    {
        var rng = new System.Random(seed ^ 0x2c1b3c6d);
        float a = (float)rng.NextDouble() * 6.28f, b = (float)rng.NextDouble() * 6.28f;
        float t = loopY / LoopLength * Mathf.PI * 2;
        return 1.9f * Mathf.Sin(t + a) + .9f * Mathf.Sin(2 * t + b) + .35f * Mathf.Sin(5 * t + a * 2);
    }

    static float RoadHalf(int seed, float loopY) => .62f + .14f * Mathf.Sin(loopY / LoopLength * Mathf.PI * 6 + seed % 7);

    static Vector2[] Rect(Vector2 centre, float w, float d, float angle)
    {
        float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
        Vector2 Corner(float x, float y) => centre + new Vector2(x * c - y * s, x * s + y * c);
        return new[] { Corner(-w / 2, -d / 2), Corner(w / 2, -d / 2), Corner(w / 2, d / 2), Corner(-w / 2, d / 2) };
    }

    static Vector2[] Blob(Vector2 centre, float radius, int sides, System.Random rng, float roughness = .35f)
    {
        var points = new Vector2[sides];
        float turn = (float)rng.NextDouble() * 6.28f;
        for (int i = 0; i < sides; i++)
        {
            float a = turn + i * Mathf.PI * 2 / sides;
            float r = radius * (1 - roughness * .5f + roughness * (float)rng.NextDouble());
            points[i] = centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
        }
        return points;
    }

    // ---------------------------------------------------------------- Ground

    public static Mesh Ground(int seed, int section)
    {
        var rng = new System.Random(seed * 31 + section * 7907 + 3);
        float R(float a, float b) => a + (b - a) * (float)rng.NextDouble();
        var b = new Builder();
        // Ash ground, gently varied in long bands.
        for (float y = -10; y < 10 - .01f; y += 1)
        {
            var tone = Color.Lerp(Ash, Dirt, .25f + .2f * Mathf.Sin(Loop(section, y) * .21f + seed));
            b.Flat(tone, 0, new Vector2(-Half, y), new Vector2(Half, y), new Vector2(Half, y + 1), new Vector2(-Half, y + 1));
        }
        // Broken concrete slabs, bare dirt and scorch patches.
        for (int i = 0; i < 54; i++)
        {
            var at = new Vector2(R(-10, 10), R(-9.4f, 9.4f));
            int kind = rng.Next(5);
            var colour = kind < 2 ? Color.Lerp(Slab, Ash, R(.1f, .5f)) : kind < 4 ? Color.Lerp(Dirt, Ash, R(.2f, .5f)) : Color.Lerp(Scorch, Ash, R(.2f, .45f));
            b.Flat(colour, .004f, Blob(at, R(.22f, .7f), rng.Next(5, 8), rng, .5f));
        }
        // The road: asphalt with crumbling shoulders, potholes, cracks and faded markings.
        const float Step = .5f;
        for (float y = -10; y < 10 - .01f; y += Step)
        {
            float l0 = Loop(section, y), l1 = Loop(section, y + Step);
            float x0 = RoadX(seed, l0), x1 = RoadX(seed, l1), h0 = RoadHalf(seed, l0), h1 = RoadHalf(seed, l1);
            float j0 = .1f * Mathf.Sin(l0 * 5.3f), j1 = .1f * Mathf.Sin(l1 * 5.3f);
            b.Flat(Color.Lerp(Asphalt, Ash, .45f), .008f, new Vector2(x0 - h0 - .22f + j0, y), new Vector2(x0 + h0 + .2f - j0, y), new Vector2(x1 + h1 + .2f - j1, y + Step), new Vector2(x1 - h1 - .22f + j1, y + Step));
            b.Flat(Asphalt, .012f, new Vector2(x0 - h0, y), new Vector2(x0 + h0, y), new Vector2(x1 + h1, y + Step), new Vector2(x1 - h1, y + Step));
            // A faded centre dash on roughly every other step, missing where it has worn away.
            if (Mathf.Repeat(l0, 2) < 1 && Mathf.Sin(l0 * 1.7f + seed) > -.4f)
                b.Flat(Marking * .8f, .016f, new Vector2(x0 - .035f, y + .1f), new Vector2(x0 + .035f, y + .1f), new Vector2(x1 + .035f, y + Step - .1f), new Vector2(x1 - .035f, y + Step - .1f));
        }
        for (int i = 0; i < 9; i++)
        {
            float y = R(-9.5f, 9.5f), l = Loop(section, y);
            var at = new Vector2(RoadX(seed, l) + R(-.4f, .4f) * RoadHalf(seed, l), y);
            b.Flat(Crack, .018f, Blob(at, R(.1f, .24f), 6, rng, .6f));
        }
        for (int i = 0; i < 40; i++)
        {
            var from = new Vector2(R(-10, 10), R(-9.6f, 9.6f));
            float angle = R(0, 6.28f);
            var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            for (int k = 0; k < 3; k++)
            {
                var to = from + dir * R(.25f, .6f);
                var side = new Vector2(-dir.y, dir.x) * .018f;
                b.Flat(Crack, .02f, from - side, from + side, to + side, to - side);
                from = to;
                dir = (dir + new Vector2(R(-.6f, .6f), R(-.6f, .6f))).normalized;
            }
        }
        // Craters: a pale thrown rim, a dark scorched bowl and a few clods.
        int craters = rng.Next(2, 5);
        for (int i = 0; i < craters; i++)
        {
            var at = new Vector2(R(-8.5f, 8.5f), R(-8.5f, 8.5f));
            float r = R(.45f, 1.1f);
            b.Flat(Color.Lerp(Slab, Dirt, .4f), .022f, Blob(at, r * 1.3f, 9, rng, .3f));
            b.Flat(Color.Lerp(Scorch, Dirt, .25f), .026f, Blob(at, r, 9, rng, .25f));
            b.Flat(Scorch * .8f, .03f, Blob(at + new Vector2(.05f, -.05f), r * .55f, 7, rng, .3f));
        }
        // Rubble fields: small chips of concrete and brick.
        for (int i = 0; i < 260; i++)
        {
            var at = new Vector2(R(-10.5f, 10.5f), R(-9.8f, 9.8f));
            var colour = Masonry[rng.Next(Masonry.Length)] * R(.7f, 1.05f);
            colour.a = 1;
            b.Flat(colour, .034f, Blob(at, R(.04f, .11f), rng.Next(3, 6), rng, .7f));
        }
        return b.Finish("City ruins ground " + section);
    }

    // ---------------------------------------------------------------- Ruins

    public static Mesh Build(int seed, int section, out Mesh shadows)
    {
        var rng = new System.Random(seed * 131 + section * 104729 + 11);
        float R(float a, float b) => a + (b - a) * (float)rng.NextDouble();
        var b = new Builder();
        var placed = new List<(Vector2 at, float radius)>();
        bool Free(Vector2 at, float radius)
        {
            float l = Loop(section, at.y);
            if (Mathf.Abs(at.x - RoadX(seed, l)) < RoadHalf(seed, l) + radius + .3f)
                return false;
            foreach (var (other, size) in placed)
                if ((other - at).sqrMagnitude < (radius + size + .12f) * (radius + size + .12f))
                    return false;
            return true;
        }
        // A broken overpass span across the road in some sections.
        if (rng.NextDouble() < .35)
            Overpass(b, seed, section, rng, placed);
        int target = rng.Next(18, 25);
        for (int attempt = 0; attempt < 420 && placed.Count < target; attempt++)
        {
            float size = R(.6f, 1.55f);
            var at = new Vector2(R(-10.2f, 10.2f), R(-9.3f, 9.3f));
            if (!Free(at, size))
                continue;
            placed.Add((at, size));
            float angle = R(-.6f, .6f) + (rng.Next(4) * Mathf.PI * .5f);
            var masonry = Masonry[rng.Next(Masonry.Length)];
            double roll = rng.NextDouble();
            RubbleSkirt(b, at, size, masonry, rng);
            if (roll < .46)
                GuttedBlock(b, at, size, angle, masonry, rng);
            else if (roll < .66)
                Skeleton(b, at, size, angle, rng);
            else if (roll < .84)
                Mound(b, at, size, masonry, rng);
            else if (roll < .94)
                WallStumps(b, at, size, angle, masonry, rng);
            else
                FallenTower(b, at, size, angle, masonry, rng);
        }
        // Wrecks and obstacles on and beside the road.
        for (int i = 0; i < 4; i++)
        {
            float y = R(-9, 9), l = Loop(section, y);
            float offset = R(-.9f, .9f) * RoadHalf(seed, l);
            var at = new Vector2(RoadX(seed, l) + offset, y);
            if (rng.NextDouble() < .7)
                BurntCar(b, at, R(0, 6.28f), rng);
            else
                TankTrap(b, at, rng);
        }
        for (int i = 0; i < 5; i++)
        {
            var at = new Vector2(R(-10, 10), R(-9.4f, 9.4f));
            if (Free(at, .25f))
                TankTrap(b, at, rng);
        }
        var ruins = b.Finish("City ruins " + section);
        shadows = b.FinishShadows("City ruin shadows " + section);
        return ruins;
    }

    // Broken masonry spilled around a ruin's foot.
    static void RubbleSkirt(Builder b, Vector2 at, float size, Color masonry, System.Random rng)
    {
        float R(float a, float c) => a + (c - a) * (float)rng.NextDouble();
        int chips = rng.Next(6, 11);
        for (int i = 0; i < chips; i++)
        {
            float angle = R(0, 6.28f), reach = size * R(.75f, 1.25f);
            var p = at + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * reach;
            b.Prism(Rect(p, R(.08f, .22f), R(.07f, .18f), R(0, 3.14f)), 0, R(.03f, .1f), Sooted(Color.Lerp(masonry, Slab, R(0, .4f)), R(.1f, .5f)));
        }
    }

    static Color Sooted(Color c, float amount)
    {
        var mixed = Color.Lerp(c, Soot, amount);
        mixed.a = 1;
        return mixed;
    }

    // A gutted block: perimeter walls with broken, uneven tops, a few surviving floor slabs
    // jutting out at their levels, dark window holes on the walls facing the viewer, rubble
    // heaped inside and, now and then, a fire still burning.
    static void GuttedBlock(Builder b, Vector2 at, float size, float angle, Color masonry, System.Random rng)
    {
        float R(float a, float c) => a + (c - a) * (float)rng.NextDouble();
        float w = size * R(1.3f, 1.8f), d = size * R(1.1f, 1.6f), height = R(.6f, 2.3f) * (.75f + size * .45f);
        var corners = Rect(at, w, d, angle);
        const float Thick = .1f, Floor = .32f;
        for (int side = 0; side < 4; side++)
        {
            var a = corners[side];
            var c = corners[(side + 1) % 4];
            float length = Vector2.Distance(a, c);
            int segments = Mathf.Max(2, Mathf.RoundToInt(length / .22f));
            var along = (c - a) / segments;
            var inward = new Vector2(-along.y, along.x).normalized * Thick;
            // One stretch of every block has fallen away.
            int gapStart = rng.Next(segments), gapLength = rng.NextDouble() < .55 ? rng.Next(1, Mathf.Max(2, segments / 2)) : 0;
            float sideHeight = height * R(.6f, 1);
            for (int s = 0; s < segments; s++)
            {
                bool gap = s >= gapStart && s < gapStart + gapLength;
                float h = gap ? R(.04f, .14f) : sideHeight * Mathf.Clamp01(.45f + .55f * Mathf.PerlinNoise(s * .45f + side * 7, (float)rng.NextDouble() * 3)) + R(-.08f, .08f);
                var p = a + along * s;
                var q = p + along;
                var colour = Sooted(masonry, Mathf.Clamp01(R(0, .35f) + (h > height * .7f ? .25f : 0)));
                b.Prism(new[] { p, q, q + inward, p + inward }, 0, Mathf.Max(.04f, h), colour);
                // Window holes on the faces turned toward the viewer (down the screen).
                var outward = -inward.normalized;
                if (!gap && outward.y < -.3f)
                    for (float z = Floor * .5f; z + .12f < h; z += Floor)
                        if (s % 2 == 0 || rng.NextDouble() < .3)
                        {
                            var m = (p + q) * .5f + outward * .004f;
                            var half = along.normalized * Mathf.Min(.07f, along.magnitude * .35f);
                            b.Face(Hole, new Vector3(m.x - half.x, m.y - half.y, z), new Vector3(m.x + half.x, m.y + half.y, z),
                                new Vector3(m.x + half.x, m.y + half.y, z + .12f), new Vector3(m.x - half.x, m.y - half.y, z + .12f));
                        }
            }
        }
        // Surviving floors: partial slabs at their levels, one jutting past a fallen wall.
        for (float z = Floor; z < height - .1f; z += Floor)
        {
            if (rng.NextDouble() < .45)
                continue;
            float fw = w * R(.35f, .85f), fd = d * R(.35f, .85f);
            var shift = new Vector2(R(-.25f, .25f) * w, R(-.25f, .25f) * d);
            float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
            var centre = at + new Vector2(shift.x * c - shift.y * s, shift.x * s + shift.y * c);
            var slabColour = Sooted(Color.Lerp(masonry, Slab, .5f), R(.1f, .45f));
            b.Prism(Rect(centre, fw, fd, angle + R(-.08f, .08f)), z, z + .05f, slabColour * .8f, slabColour);
        }
        // Rubble heaped inside.
        for (int i = 0; i < 6; i++)
        {
            var p = at + new Vector2(R(-.3f, .3f) * w, R(-.3f, .3f) * d);
            b.Prism(Rect(p, R(.12f, .3f), R(.1f, .26f), R(0, 3.14f)), 0, R(.05f, .2f), Sooted(masonry, R(.2f, .6f)));
        }
        if (rng.NextDouble() < .38)
            Fire(b, at + new Vector2(R(-.2f, .2f) * w, R(-.2f, .2f) * d), R(.05f, .3f), rng);
    }

    // A bare tower skeleton: corner and mid columns standing through broken floor plates,
    // stepping down where the top has been blown off.
    static void Skeleton(Builder b, Vector2 at, float size, float angle, System.Random rng)
    {
        float R(float a, float c) => a + (c - a) * (float)rng.NextDouble();
        float w = size * R(1.3f, 1.7f), d = size * R(1.3f, 1.7f), height = R(1.9f, 3.5f);
        float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
        Vector2 Local(float x, float y) => at + new Vector2(x * c - y * s, x * s + y * c);
        var concrete = Color.Lerp(Slab, Masonry[rng.Next(Masonry.Length)], .3f);
        for (int ix = 0; ix < 3; ix++)
            for (int iy = 0; iy < 3; iy++)
            {
                if (ix == 1 && iy == 1)
                    continue;
                float x = (ix - 1) * w * .45f, y = (iy - 1) * d * .45f;
                // The blown-off corner leans the skyline down to one side.
                float h = height * (1 - .45f * (ix + iy) / 4f) * R(.75f, 1);
                b.Prism(Rect(Local(x, y), .1f, .1f, angle), 0, h, Sooted(concrete, R(.15f, .5f)));
            }
        for (float z = .34f; z < height; z += .34f)
        {
            float cut = 1 - z / height;
            if (rng.NextDouble() < .25)
                continue;
            float fw = w * Mathf.Lerp(.5f, 1, cut) * R(.8f, 1), fd = d * Mathf.Lerp(.45f, 1, cut) * R(.8f, 1);
            var offset = Local(-(w - fw) * .45f, -(d - fd) * .45f) - at;
            b.Prism(Rect(at + offset, fw, fd, angle), z, z + .045f, Sooted(concrete, R(.2f, .5f)) * .85f, Sooted(concrete, R(.1f, .4f)));
        }
    }

    // A collapsed building: a low mound of slabs and blocks, some tilted up out of it.
    static void Mound(Builder b, Vector2 at, float size, Color masonry, System.Random rng)
    {
        float R(float a, float c) => a + (c - a) * (float)rng.NextDouble();
        b.Prism(Blob(at, size * .9f, 8, rng, .4f), 0, R(.08f, .16f), Sooted(masonry, R(.2f, .4f)));
        b.Prism(Blob(at + new Vector2(R(-.2f, .2f), R(-.2f, .2f)) * size, size * .55f, 7, rng, .4f), 0, R(.2f, .36f), Sooted(masonry, R(.25f, .5f)));
        for (int i = 0; i < 9; i++)
        {
            var p = at + new Vector2(R(-.7f, .7f), R(-.7f, .7f)) * size;
            b.Prism(Rect(p, R(.15f, .45f), R(.08f, .2f), R(0, 3.14f)), 0, R(.12f, .5f), Sooted(Color.Lerp(masonry, Slab, R(0, .5f)), R(.1f, .5f)));
        }
        if (rng.NextDouble() < .25)
            Fire(b, at, R(.15f, .3f), rng);
    }

    // Wall stumps: what is left of a low building, a few broken lengths of wall.
    static void WallStumps(Builder b, Vector2 at, float size, float angle, Color masonry, System.Random rng)
    {
        float R(float a, float c) => a + (c - a) * (float)rng.NextDouble();
        var corners = Rect(at, size * 1.6f, size * 1.3f, angle);
        for (int side = 0; side < 4; side++)
        {
            if (rng.NextDouble() < .35)
                continue;
            var a = corners[side];
            var c = corners[(side + 1) % 4];
            float from = R(0, .4f), to = R(.55f, 1);
            var p = Vector2.Lerp(a, c, from);
            var q = Vector2.Lerp(a, c, to);
            var inward = new Vector2(-(q - p).y, (q - p).x).normalized * .09f;
            b.Prism(new[] { p, q, q + inward, p + inward }, 0, R(.12f, .42f), Sooted(masonry, R(.1f, .4f)));
        }
        for (int i = 0; i < 4; i++)
            b.Prism(Rect(at + new Vector2(R(-.5f, .5f), R(-.5f, .5f)) * size, R(.1f, .25f), R(.1f, .2f), R(0, 3.14f)), 0, R(.05f, .14f), Sooted(masonry, .4f));
    }

    // A tower brought down whole, lying across the ground with its floors showing.
    static void FallenTower(Builder b, Vector2 at, float size, float angle, Color masonry, System.Random rng)
    {
        float R(float a, float c) => a + (c - a) * (float)rng.NextDouble();
        float length = size * R(2.2f, 3), width = size * .7f;
        var body = Rect(at, length, width, angle);
        b.Prism(body, 0, R(.32f, .48f), Sooted(masonry, R(.2f, .4f)));
        float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
        var axis = new Vector2(c, s);
        for (float t = -length * .4f; t < length * .45f; t += .3f)
        {
            var p = at + axis * t;
            b.Prism(Rect(p, .04f, width * 1.04f, angle), .32f, .5f, Sooted(masonry, .5f) * .7f);
        }
        b.Prism(Blob(at + axis * length * .55f, width * .7f, 7, rng, .5f), 0, .22f, Sooted(masonry, .45f));
    }

    // A broken elevated highway: a deck on pillars crossing the road, snapped in the middle.
    static void Overpass(Builder b, int seed, int section, System.Random rng, List<(Vector2, float)> placed)
    {
        float R(float a, float c) => a + (c - a) * (float)rng.NextDouble();
        float y = R(-6, 6);
        float centre = RoadX(seed, Loop(section, y));
        float angle = R(-.5f, .5f);
        var axis = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        float gap = R(.6f, 1.4f), deck = .9f, height = R(1.0f, 1.3f);
        foreach (int side in new[] { -1, 1 })
        {
            float start = gap * .5f, end = R(4.2f, 7);
            var from = new Vector2(centre, y) + axis * side * start;
            var to = new Vector2(centre, y) + axis * side * end;
            var mid = (from + to) * .5f;
            float span = end - start;
            b.Prism(Rect(mid, span, deck, angle), height, height + .14f, C(.40f, .40f, .39f), C(.46f, .46f, .44f));
            // The snapped end sags in a jagged lip of rebar and slab.
            b.Prism(Rect(from + axis * side * .2f, .4f, deck * .8f, angle + side * .3f), height - .18f, height + .02f, Sooted(Slab, .5f));
            for (float t = 1.2f; t < span; t += 1.7f)
            {
                var pillar = from + axis * side * t;
                b.Prism(Rect(pillar, .16f, .5f, angle), 0, height, C(.36f, .36f, .35f));
                placed.Add((pillar, .35f));
            }
            // The deck's shadow, cast from its full height.
            b.Shadow(Rect(mid, span, deck, angle), height + .14f);
        }
    }

    static void BurntCar(Builder b, Vector2 at, float angle, System.Random rng)
    {
        float R(float a, float c) => a + (c - a) * (float)rng.NextDouble();
        var hull = Color.Lerp(Rust, Steel, R(0, .6f));
        b.Prism(Rect(at, .44f, .22f, angle), 0, .1f, hull * .8f);
        var axis = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        b.Prism(Rect(at - axis * .03f, .24f, .18f, angle), .1f, .17f, Sooted(hull, .5f), Hole);
    }

    // A steel hedgehog: three crossed girders.
    static void TankTrap(Builder b, Vector2 at, System.Random rng)
    {
        float turn = (float)rng.NextDouble() * 3.14f;
        for (int i = 0; i < 3; i++)
            b.Prism(Rect(at, .32f, .045f, turn + i * Mathf.PI / 3), 0, .14f + .04f * i, Steel * (1 - .1f * i));
    }

    // A small fire: a warm scorch glow on the ground and flame tongues licking up.
    static void Fire(Builder b, Vector2 at, float z, System.Random rng)
    {
        float R(float a, float c) => a + (c - a) * (float)rng.NextDouble();
        var glow = new Color(.75f, .32f, .1f, 1) * .55f;
        glow.a = 1;
        b.Flat(glow, .04f, Blob(at, R(.18f, .28f), 7, rng, .3f));
        for (int i = 0; i < 3; i++)
        {
            var p = at + new Vector2(R(-.09f, .09f), R(-.06f, .06f));
            float h = R(.16f, .3f), w = R(.05f, .08f);
            b.Face(Flame, new Vector3(p.x - w, p.y, z), new Vector3(p.x + w, p.y, z), new Vector3(p.x + R(-.03f, .03f), p.y, z + h));
            b.Face(FlameCore, new Vector3(p.x - w * .45f, p.y - .002f, z), new Vector3(p.x + w * .45f, p.y - .002f, z), new Vector3(p.x, p.y - .002f, z + h * .55f));
        }
    }

    // ---------------------------------------------------------------- Near layer

    // The parallax foreground frames the flight lane with dark broken walls and girders at the
    // screen edges only, so the playfield stays clear.
    public static Mesh Near(int seed, int section)
    {
        var rng = new System.Random(seed * 17 + section * 3571 + 5);
        float R(float a, float c) => a + (c - a) * (float)rng.NextDouble();
        var b = new Builder();
        var dark = C(.10f, .10f, .11f);
        foreach (int side in new[] { -1, 1 })
            for (int i = 0; i < 3; i++)
            {
                var at = new Vector2(side * R(7.4f, 9.6f), R(-9, 9));
                if (rng.NextDouble() < .5)
                    b.Prism(Rect(at, R(.6f, 1.4f), .12f, R(-.4f, .4f) + Mathf.PI * .5f * rng.Next(2)), 0, R(.6f, 1.4f), dark);
                else
                    b.Prism(Rect(at, R(1.2f, 2.2f), .07f, R(0, 3.14f)), R(.4f, .9f), R(.95f, 1.1f), dark * 1.2f);
            }
        return b.Finish("City ruins foreground " + section);
    }
}
