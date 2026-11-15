using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

// Buildings retain the street grid at section seams; block use and architecture vary with the run seed.
// Each section is its own neighbourhood: a downtown of towers around a seeded centre, a mixed
// midtown, low terraced streets with trees and parks, or an industrial quarter. A tall block
// (a stadium, a park or a rail station) can span one quiet cross street. Every building casts
// a long sun shadow onto the streets, so the skyline reads as height rather than a flat plan.
public static class CityDistrictGeometry
{
    // Roofs lean up and right with height; the sun throws shadows down and right.
    const float LeanX = .14f, LeanY = .24f;
    static readonly Vector2 ShadowPerHeight = new Vector2(.38f, -.26f);
    const float ShadowPlane = .125f;
    static readonly Color ShadowColor = new Color(.02f, .035f, .07f, .36f);

    enum Quarter
    {
        Downtown, Midtown, Terraces, Industry
    }

    sealed class Builder
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<Color> colors = new List<Color>();
        readonly List<int> triangles = new List<int>();
        readonly List<Vector3> shade = new List<Vector3>();
        readonly List<Color> shadeColors = new List<Color>();
        readonly List<int> shadeTriangles = new List<int>();
        Vector2 blockCenter;
        bool quarterTurn;
        public void OrientBlock(float x, float y, bool turn)
        {
            blockCenter = new Vector2(x, y);
            quarterTurn = turn;
        }

        Vector3 Turn(Vector3 p) => quarterTurn ? new Vector3(blockCenter.x - (p.y - blockCenter.y), blockCenter.y + (p.x - blockCenter.x), p.z) : p;
        static Vector3 Lean(Vector3 q) => new Vector3(q.x + q.z * LeanX, q.y + q.z * LeanY, -q.z);

        void Face(Color c, params Vector3[] points)
        {
            c.a = 1; // Facade shading changes light, never the solidity of a building.
            int first = vertices.Count;
            foreach (var p in points)
            {
                vertices.Add(Lean(Turn(p)));
                colors.Add(c);
            }
            for (int i = 1; i < points.Length - 1; i++)
            {
                triangles.Add(first);
                triangles.Add(first + i);
                triangles.Add(first + i + 1);
            }
        }

        // The shadow of a ground-standing footprint: the hull of its base and its sun-shifted top.
        void Shadow(float x, float y, float w, float d, float top)
        {
            if (top < .16f)
                return;
            var points = new List<Vector2>(8);
            var shift = ShadowPerHeight * top;
            foreach (var corner in new[] { new Vector2(-w, -d), new Vector2(w, -d), new Vector2(w, d), new Vector2(-w, d) })
            {
                var p = (Vector2)Turn(new Vector3(x + corner.x * .5f, y + corner.y * .5f));
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

        public void Box(float x, float y, float z, float w, float d, float h, Color c)
        {
            float a = x - w / 2, b = x + w / 2, f = y - d / 2, k = y + d / 2, t = z + h;
            Face(c * .65f, new Vector3(a, f, z), new Vector3(b, f, z), new Vector3(b, f, t), new Vector3(a, f, t));
            Face(c * .78f, new Vector3(b, f, z), new Vector3(b, k, z), new Vector3(b, k, t), new Vector3(b, f, t));
            Face(c * .87f, new Vector3(a, k, z), new Vector3(a, f, z), new Vector3(a, f, t), new Vector3(a, k, t));
            Face(c, new Vector3(a, f, t), new Vector3(b, f, t), new Vector3(b, k, t), new Vector3(a, k, t));
            if (z < .2f)
                Shadow(x, y, w, d, t);
        }

        // A flat top only, for markings and surfaces that never show their sides.
        public void Plate(float x, float y, float z, float w, float d, Color c)
        {
            float a = x - w / 2, b = x + w / 2, f = y - d / 2, k = y + d / 2;
            Face(c, new Vector3(a, f, z), new Vector3(b, f, z), new Vector3(b, k, z), new Vector3(a, k, z));
        }

        public void WindowBand(float x, float y, float z, float w, float d, float h, Color c)
        {
            float a = x - w * .42f, b = x + w * .42f;
            float f = y - d * .5f - .003f, k = y + d * .42f;
            Face(c * .8f, new Vector3(a, f, z), new Vector3(b, f, z), new Vector3(b, f, z + h), new Vector3(a, f, z + h));
            float side = x + w * .5f + .003f;
            Face(c, new Vector3(side, y - d * .42f, z), new Vector3(side, k, z), new Vector3(side, k, z + h), new Vector3(side, y - d * .42f, z + h));
        }

        // Vertical mullions on the two faces the camera sees, for tall glass towers.
        public void Mullions(float x, float y, float z, float w, float d, float h, Color c, int count)
        {
            float f = y - d * .5f - .004f, left = x - w * .5f - .004f;
            for (int i = 1; i < count; i++)
            {
                float u = x - w * .5f + w * i / count;
                Face(c, new Vector3(u - .012f, f, z), new Vector3(u + .012f, f, z), new Vector3(u + .012f, f, z + h), new Vector3(u - .012f, f, z + h));
                float v = y - d * .5f + d * i / count;
                Face(c * .9f, new Vector3(left, v - .012f, z), new Vector3(left, v + .012f, z), new Vector3(left, v + .012f, z + h), new Vector3(left, v - .012f, z + h));
            }
        }

        public void Parapet(float x, float y, float z, float w, float d, float h, Color c)
        {
            float a = x - w * .49f, b = x + w * .49f;
            float f = y - d * .49f, k = y + d * .49f;
            Face(c * .72f, new Vector3(a, f, z), new Vector3(b, f, z), new Vector3(b, f, z + h), new Vector3(a, f, z + h));
            Face(c * .85f, new Vector3(b, f, z), new Vector3(b, k, z), new Vector3(b, k, z + h), new Vector3(b, f, z + h));
            Face(c, new Vector3(b, k, z), new Vector3(a, k, z), new Vector3(a, k, z + h), new Vector3(b, k, z + h));
            Face(c * .9f, new Vector3(a, k, z), new Vector3(a, f, z), new Vector3(a, f, z + h), new Vector3(a, k, z + h));
        }

        public void Cylinder(float x, float y, float z, float radius, float h, Color c, int sides = 10)
        {
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides, b = (i + 1) * Mathf.PI * 2 / sides;
                var p = new Vector3(x + Mathf.Cos(a) * radius, y + Mathf.Sin(a) * radius, z);
                var q = new Vector3(x + Mathf.Cos(b) * radius, y + Mathf.Sin(b) * radius, z);
                Face(c * (.65f + .25f * i / sides), p, q, q + Vector3.forward * h, p + Vector3.forward * h);
                Face(c, new Vector3(x, y, z + h), p + Vector3.forward * h, q + Vector3.forward * h);
            }
            if (z < .2f)
                Shadow(x, y, radius * 1.6f, radius * 1.6f, z + h);
        }

        public void Chamfer(float x, float y, float z, float w, float d, float h, Color c)
        {
            float cut = Mathf.Min(w, d) * .18f;
            var ring = new[]{new Vector2(-w/2+cut,-d/2),new Vector2(w/2-cut,-d/2),new Vector2(w/2,-d/2+cut),new Vector2(w/2,d/2-cut),
                new Vector2(w/2-cut,d/2),new Vector2(-w/2+cut,d/2),new Vector2(-w/2,d/2-cut),new Vector2(-w/2,-d/2+cut)};
            var top = new Vector3[ring.Length];
            for (int i = 0; i < ring.Length; i++)
            {
                var a = new Vector3(x + ring[i].x, y + ring[i].y, z);
                var q = ring[(i + 1) % ring.Length];
                var b = new Vector3(x + q.x, y + q.y, z);
                Face(c * (i < 3 ? .66f : .83f), a, b, b + Vector3.forward * h, a + Vector3.forward * h);
                top[i] = a + Vector3.forward * h;
            }
            Face(c, top);
            if (z < .2f)
                Shadow(x, y, w * .95f, d * .95f, z + h);
        }

        public void Ridge(float x, float y, float z, float w, float d, float rise, Color c)
        {
            var a = new Vector3(x - w / 2, y - d / 2, z);
            var q = new Vector3(x + w / 2, y - d / 2, z);
            var r = new Vector3(x + w / 2, y + d / 2, z);
            var s = new Vector3(x - w / 2, y + d / 2, z);
            var front = new Vector3(x, y - d / 2, z + rise);
            var back = new Vector3(x, y + d / 2, z + rise);
            Face(c * .72f, a, q, front);
            Face(c * .86f, s, a, front, back);
            Face(c, q, r, back, front);
            Face(c * .7f, r, s, back);
        }

        // A small street or park tree: a dark trunk base and a two-tier faceted canopy.
        public void Tree(float x, float y, float z, float radius, Color leaf)
        {
            Cylinder(x, y, z, radius, radius * .9f, leaf * .82f, 6);
            Cylinder(x - radius * .12f, y + radius * .1f, z + radius * .9f, radius * .7f, radius * .55f, leaf, 6);
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

    static Color C(float r, float g, float b) => new Color(r, g, b, 1);
    static int BlockHash(int seed, int x, int y)
    {
        unchecked
        {
            uint h = (uint)seed ^ (uint)x * 374761393u ^ (uint)y * 668265263u;
            h = (h ^ (h >> 13)) * 1274126177u;
            return (int)((h ^ (h >> 16)) & 0x7fffffff);
        }
    }

    // Roof materials: grey membrane, dark tar, warm gravel, pale coating and planted roofs.
    static readonly Color[] RoofTones =
    {
        C(.40f, .42f, .43f), C(.20f, .22f, .24f), C(.44f, .40f, .34f), C(.56f, .57f, .55f), C(.25f, .38f, .26f)
    };
    static readonly Color Paving = C(.29f, .30f, .295f);
    static readonly Color Leaf = C(.20f, .40f, .24f);

    // The loop positions of the cross streets that carry traffic; tall blocks never cover them.
    static bool BusyCrossStreet(int section, float localY)
    {
        float loop = (section - 1) * 20 + localY;
        for (int j = 0; j < 8; j++)
        {
            // Horizontal cars run at loop heights 5, 12.5 ... 57.5 (LevelEnvironment traffic).
            float traffic = (j * 3 + 2) * 2.5f;
            if (Mathf.Abs(Mathf.Repeat(loop - traffic + 40, 80) - 40) < .3f)
                return true;
        }
        return false;
    }

    public static Mesh Build(int seed, int section) => Build(seed, section, out var shadows, false);

    public static Mesh Build(int seed, int section, out Mesh shadows) => Build(seed, section, out shadows, true);

    static Mesh Build(int seed, int section, out Mesh shadows, bool keepShadows)
    {
        var rng = new System.Random(seed + section * 7919);
        var b = new Builder();
        float R(float a, float z) => (float)(a + (z - a) * rng.NextDouble());
        var glass = C(.11f, .30f, .38f);
        int palette = BlockHash(seed, 17, 29) % 3;
        // Each section is its own neighbourhood, drawn from the seed.
        var quarter = (Quarter)(BlockHash(seed, 41, section) % 4);
        var centre = new Vector2(R(-3.2f, 3.2f), R(-5, 5));
        float Towering(float x, float y)
        {
            float dx = x - centre.x, dy = y - centre.y;
            float field = Mathf.Exp(-(dx * dx / 22 + dy * dy / 36));
            return quarter == Quarter.Downtown ? field : quarter == Quarter.Midtown ? field * .6f : quarter == Quarter.Industry ? field * .15f : 0;
        }

        // One tall block may span a quiet cross street within this section.
        var taken = new bool[8, 8];
        if (rng.NextDouble() < .75)
        {
            for (int attempt = 0; attempt < 12; attempt++)
            {
                int column = rng.Next(1, 7), row = rng.Next(0, 7);
                float crossY = -10 + (row + 1) * 2.5f;
                if (BusyCrossStreet(section, crossY))
                    continue;
                float x = -9.1f + column * 2.6f, y = -8.75f + row * 2.5f + 1.25f;
                int kind = quarter == Quarter.Terraces ? 0 : quarter == Quarter.Industry ? 2 : rng.Next(3);
                b.OrientBlock(x, y, false);
                if (kind == 0)
                    Park(b, x, y, rng);
                else if (kind == 1)
                    Stadium(b, x, y);
                else
                    RailYard(b, x, y, rng);
                taken[column, row] = taken[column, row + 1] = true;
                break;
            }
        }

        for (int row = 0; row < 8; row++)
            for (int column = 0; column < 8; column++)
            {
                if (taken[column, row])
                    continue;
                float x = -9.1f + column * 2.6f, y = -8.75f + row * 2.5f;
                b.OrientBlock(x, y, false);
                // Coarse zones make neighborhoods, not a different random building in every cell.
                int zoneHash = BlockHash(seed, column / 2, (section * 8 + row) / 2), zone = zoneHash % 4;
                float tower = Towering(x, y);
                if (quarter == Quarter.Terraces && zone == 2)
                    zone = 0;
                if (quarter == Quarter.Industry && zone != 3 && (zoneHash >> 5) % 3 != 0)
                    zone = 3;
                if (tower > .32f)
                    zone = 2;
                bool landmark = tower > .55f || column % 2 == ((zoneHash >> 3) & 1) && row % 2 == ((zoneHash >> 4) & 1);
                int use = rng.Next(100);
                b.Box(x, y, .02f, 2.05f, 1.98f, .06f, Paving);
                bool leafy = quarter == Quarter.Terraces || zone == 0 || zone == 1 && use % 2 == 0;
                if (leafy && column > 0 && column < 7)
                    StreetTrees(b, x, y, rng);
                if (use < (quarter == Quarter.Terraces ? 18 : 10) && !landmark)
                {
                    Square(b, x, y, use % 3 == 0, rng);
                    continue;
                }
                if (use < (quarter == Quarter.Industry ? 24 : 15) && !landmark)
                {
                    CarPark(b, x, y, rng);
                    continue;
                }
                // Adjacent blocks share an axis, while independent zone bits vary it between neighborhoods.
                // Streets and paving retain their section-safe grid; only the architecture turns.
                b.OrientBlock(x, y, ((zoneHash >> 11) & 1) != 0);
                int lots = landmark || zone == 3 ? 1 : zone == 0 && use > 55 ? 4 : 2;
                var districtColor = zone == 3 ? C(.33f, .40f, .36f) : zone == 2 ? C(.27f, .37f, .48f) : zone == 1 ? C(.60f, .52f, .38f) :
                    palette == 0 ? C(.58f, .33f, .25f) : palette == 1 ? C(.42f, .50f, .43f) : C(.58f, .44f, .30f);
                for (int lot = 0; lot < lots; lot++)
                {
                    float xx = x + (lots == 1 ? 0 : (lot % 2 == 0 ? -.5f : .5f));
                    float yy = y + (lots < 4 ? 0 : (lot < 2 ? -.46f : .46f));
                    float w = lots == 1 ? R(1.4f, 1.76f) : R(.70f, .86f), d = lots < 4 ? R(1.35f, 1.72f) : R(.68f, .84f);
                    float h = zone == 2 && landmark ? R(1.9f, 2.65f) : zone == 1 && landmark ? R(.9f, 1.35f) : zone == 0 ? R(.26f, .58f) : R(.42f, .84f);
                    // Downtown rises toward its centre; the outskirts stay low.
                    h *= 1 + tower * (landmark ? 1.5f : 2.4f);
                    var facade = Color.Lerp(districtColor, C(.22f, .25f, .27f), R(.0f, .22f));
                    var trim = Color.Lerp(facade, C(.70f, .67f, .55f), .35f);
                    var roof = RoofTones[(BlockHash(seed, column * 8 + lot, section * 8 + row) >> 3) % RoofTones.Length];
                    int style = zone == 3 ? (use % 2 == 0 ? 4 : 8) : zone == 2 && landmark ? (((zoneHash >> 7) & 1) == 0 ? 0 : 9) :
                        zone == 1 && landmark ? (((zoneHash >> 7) & 1) == 0 ? 3 : 2) : zone == 0 ? (use % 3 == 0 ? 1 : 5) : (use % 3 == 0 ? 0 : 6);
                    if (zone == 2 && tower > .32f && (lots == 1 || use % 3 == 0))
                        style = 10;
                    Lot(b, style, xx, yy, w, d, h, facade, trim, roof, glass, zone, palette, landmark, use + lot, rng);
                }
            }
        var architecture = b.Finish("Seeded city architecture " + section);
        shadows = keepShadows ? b.FinishShadows("City sun shadows " + section) : null;
        return architecture;
    }

    static void Lot(Builder b, int style, float xx, float yy, float w, float d, float h, Color facade, Color trim, Color roof, Color glass, int zone, int palette, bool landmark, int use, System.Random rng)
    {
        if (style == 10) // A glass skyscraper on a stone podium, crowned by a helipad, spire or plant deck.
        {
            b.Box(xx, yy, .09f, w, d, .22f, Color.Lerp(facade, C(.62f, .60f, .54f), .5f));
            float tw = w * (.54f + use % 3 * .06f), td = d * (.66f - use % 3 * .05f);
            var tints = new[] { C(.30f, .52f, .66f), C(.24f, .44f, .46f), C(.50f, .44f, .34f), C(.40f, .46f, .56f) };
            var tint = Color.Lerp(glass, tints[use % tints.Length], .55f);
            b.Box(xx, yy, .31f, tw, td, h, tint);
            b.Mullions(xx, yy, .31f, tw, td, h, Color.Lerp(tint, C(.75f, .82f, .86f), .4f), 4 + use % 3);
            b.Box(xx, yy, .31f + h, tw * .82f, td * .82f, .08f, trim);
            int crown = (use / 3) % 3;
            if (crown == 1)
            {
                b.Box(xx, yy, .39f + h, tw * .46f, td * .46f, .22f, Color.Lerp(tint, trim, .5f));
                b.Cylinder(xx, yy, .61f + h, .03f, .45f, C(.72f, .72f, .70f), 5);
                return;
            }
            if (crown == 2)
            {
                for (int unit = 0; unit < 4; unit++)
                    b.Box(xx - tw * .25f + unit % 2 * tw * .5f, yy - td * .2f + unit / 2 * td * .4f, .39f + h, tw * .22f, td * .2f, .07f, C(.62f, .63f, .62f));
                return;
            }
            float pad = Mathf.Min(tw, td) * .28f;
            var paint = C(.86f, .80f, .30f);
            b.Cylinder(xx, yy, .39f + h, pad, .012f, C(.60f, .62f, .60f), 10);
            b.Plate(xx - pad * .32f, yy, .403f + h, pad * .12f, pad * .9f, paint);
            b.Plate(xx + pad * .32f, yy, .403f + h, pad * .12f, pad * .9f, paint);
            b.Plate(xx, yy, .404f + h, pad * .64f, pad * .12f, paint);
            return;
        }
        if (style == 0)
        {
            b.Box(xx, yy, .09f, w, d, h * .50f, facade);
            b.Box(xx - w * .07f, yy + d * .06f, .09f + h * .50f, w * .78f, d * .78f, h * .29f, Color.Lerp(facade, glass, .35f));
            b.Chamfer(xx - w * .07f, yy + d * .06f, .09f + h * .79f, w * .52f, d * .57f, h * .21f, facade);
        }
        else if (style == 1)
        {
            b.Box(xx - w * .23f, yy, .09f, w * .54f, d, h, facade);
            b.Box(xx + w * .2f, yy - d * .3f, .09f, w * .45f, d * .4f, h * .72f, facade);
            b.Plate(xx + w * .2f, yy - d * .3f, .092f + h * .72f, w * .40f, d * .34f, roof);
        }
        else if (style == 2)
        {
            b.Box(xx - w * .4f, yy, .09f, w * .2f, d, h, facade);
            b.Box(xx + w * .4f, yy, .09f, w * .2f, d, h, facade);
            b.Box(xx, yy - d * .4f, .09f, w * .6f, d * .2f, h, facade);
            b.Box(xx, yy + d * .4f, .09f, w * .6f, d * .2f, h, facade);
            b.Box(xx, yy, .1f, w * .52f, d * .5f, .01f, C(.18f, .30f, .22f));
            b.Box(xx, yy, .115f, w * .10f, d * .45f, .015f, trim);
            b.Tree(xx - w * .17f, yy - d * .1f, .12f, .07f, Leaf);
            b.Tree(xx + w * .17f, yy + d * .1f, .12f, .07f, Leaf);
        }
        else if (style == 3) // A civic rotunda surrounded by a quiet low podium.
        {
            b.Box(xx, yy, .09f, w, d, .18f, C(.52f, .50f, .44f));
            b.Cylinder(xx, yy, .27f, Mathf.Min(w, d) * .44f, h * .68f, facade, 12);
            b.Cylinder(xx, yy, .27f + h * .68f, Mathf.Min(w, d) * .32f, h * .20f, C(.36f, .52f, .48f), 12);
        }
        else if (style == 4) // Industrial halls use broad pitched roof bays.
        {
            h *= .65f;
            b.Box(xx, yy, .09f, w, d, h, facade);
            for (int j = 0; j < 3; j++)
                b.Ridge(xx + (j - 1) * w * .333f, yy, .09f + h, w * .32f, d, .14f, j % 2 == 0 ? C(.42f, .44f, .45f) : C(.36f, .38f, .40f));
            if (use % 3 == 0)
                b.Cylinder(xx + w * .38f, yy + d * .36f, .09f, .07f, h + .9f, C(.48f, .40f, .34f), 8);
        }
        else if (style == 8)
        {
            h *= .65f;
            b.Box(xx, yy, .09f, w, d, h, facade);
            for (int solar = 0; solar < 2; solar++)
                b.Box(xx + (solar - .5f) * w * .35f, yy, .1f + h, w * .25f, d * .66f, .055f, C(.16f, .24f, .36f));
            for (int tank = 0; tank < 2; tank++)
                b.Cylinder(xx - w * .32f, yy + (tank - .5f) * d * .5f, .1f + h, .09f, .12f, C(.66f, .64f, .58f), 8);
        }
        else if (style == 9)
        {
            b.Chamfer(xx, yy, .09f, w, d, h * .22f, facade);
            b.Chamfer(xx, yy, .09f + h * .22f, w * .74f, d * .76f, h * .68f, Color.Lerp(facade, glass, .40f));
            b.Chamfer(xx, yy, .09f + h * .90f, w * .57f, d * .58f, h * .10f, facade);
        }
        else if (style == 5)
        {
            b.Box(xx, yy, .09f, w, d, h, facade);
            var tiles = palette == 1 ? C(.30f, .38f, .36f) : C(.56f, .29f, .21f);
            b.Ridge(xx, yy, .09f + h, w, d, .16f, Color.Lerp(tiles, C(.40f, .25f, .20f), (float)rng.NextDouble() * .4f));
        }
        else
        {
            b.Box(xx, yy, .09f, w, d, h * .65f, facade);
            b.Box(xx, yy + d * .05f, .09f + h * .65f, w * .82f, d * .80f, h * .35f, facade);
        }
        if (style == 0 || style == 6 || style == 9)
        {
            float rw = style == 0 ? w * .52f : style == 9 ? w * .57f : w * .82f, rd = style == 0 ? d * .57f : style == 9 ? d * .58f : d * .80f;
            float rx = xx - (style == 0 ? w * .07f : 0), ry = yy + (style == 0 ? d * .06f : style == 6 ? d * .05f : 0);
            float roofScale = style == 0 || style == 9 ? .72f : .94f;
            b.Box(rx, ry, .09f + h, rw * roofScale, rd * roofScale, .025f, roof);
            b.Parapet(rx, ry, .115f + h, style == 9 || style == 0 ? rw * .72f : rw, style == 9 || style == 0 ? rd * .72f : rd, .075f, trim);
            int roofUse = (use + zone) % 5;
            if (roofUse == 0)
            {
                b.Box(rx - rw * .10f, ry, .12f + h, rw * .36f, rd * .35f, .13f, trim);
                b.Box(rx - rw * .10f, ry, .25f + h, rw * .24f, rd * .24f, .025f, roof);
            }
            else if (roofUse == 1)
            {
                float tankRadius = Mathf.Min(rw, rd) * .14f;
                b.Box(rx, ry, .12f + h, tankRadius * 2.2f, tankRadius * 2.2f, .08f, roof);
                b.Cylinder(rx, ry, .20f + h, tankRadius, .20f, C(.48f, .36f, .26f), 6);
                b.Cylinder(rx, ry, .40f + h, tankRadius * .82f, .025f, C(.30f, .24f, .20f), 6);
            }
            else if (roofUse == 2)
            {
                b.Box(rx, ry, .12f + h, rw * .62f, rd * .48f, .045f, trim);
                b.Box(rx, ry, .165f + h, rw * .52f, rd * .38f, .035f, C(.20f, .38f, .25f));
                b.Tree(rx - rw * .16f, ry, .20f + h, .05f, Leaf);
                b.Tree(rx + rw * .16f, ry, .20f + h, .05f, Leaf);
            }
            else if (roofUse == 3)
            {
                // A cluster of air-handling units.
                for (int unit = 0; unit < 3; unit++)
                    b.Box(rx - rw * .22f + unit * rw * .2f, ry - rd * .15f, .12f + h, rw * .14f, rd * .18f, .06f, C(.60f, .61f, .60f));
            }
            else
            {
                b.Cylinder(rx, ry, .12f + h, .025f, .32f, trim, 5);
                b.Box(rx, ry, .34f + h, rw * .30f, .025f, .025f, trim);
            }
        }
        if (style == 0 || style == 6)
            for (float floor = .25f; floor + .07f < h; floor += .31f)
            {
                if (style == 0 && ((floor < h * .50f && floor + .07f > h * .50f) || (floor < h * .79f && floor + .07f > h * .79f)))
                    continue;
                if (style == 6 && floor < h * .65f && floor + .07f > h * .65f)
                    continue;
                float fw = style == 0 ? floor > h * .79f ? w * .52f : floor > h * .50f ? w * .78f : w : floor > h * .65f ? w * .82f : w;
                float fd = style == 0 ? floor > h * .79f ? d * .57f : floor > h * .50f ? d * .78f : d : floor > h * .65f ? d * .80f : d;
                float shiftX = style == 0 && floor > h * .5f ? -w * .07f : 0;
                float shiftY = style == 0 && floor > h * .5f ? d * .06f : style == 6 && floor > h * .65f ? d * .05f : 0;
                if (style != 0 || floor < h * .79f)
                    b.WindowBand(xx + shiftX, yy + shiftY, .09f + floor, fw, fd, .07f, Color.Lerp(facade, glass, .7f));
            }
        if (style == 1)
            b.WindowBand(xx - w * .23f, yy, .09f + h * .40f, w * .54f, d, .065f, glass);
        if (style == 5 || style == 8)
            b.WindowBand(xx, yy, .09f + h * .42f, w, d, .075f, glass);
        if (style == 1 || style == 5 || style == 6)
        {
            var awning = zone == 0 ? C(.24f, .44f, .38f) : use % 2 == 0 ? C(.62f, .30f, .22f) : C(.70f, .58f, .26f);
            b.Box(xx, yy - d * .49f, .25f, w * .72f, .23f, .055f, awning);
            foreach (int s in new[] { -1, 1 })
                b.Box(xx + s * w * .31f, yy - d * .55f, .10f, .035f, .035f, .15f, trim);
        }
        if (landmark && style == 0)
        {
            b.Box(xx - w * .3f, yy - d * .2f, .11f + h * .5f, w * .18f, d * .25f, .08f, C(.20f, .38f, .26f));
            b.Box(xx + w * .27f, yy + d * .24f, .11f + h * .5f, w * .17f, d * .22f, .08f, C(.23f, .42f, .29f));
        }
    }

    // Trees line both long kerbs of a block.
    static void StreetTrees(Builder b, float x, float y, System.Random rng)
    {
        foreach (int side in new[] { -1, 1 })
            for (float along = -.78f; along <= .79f; along += .39f)
            {
                if (rng.NextDouble() < .22)
                    continue;
                float radius = .075f + (float)rng.NextDouble() * .03f;
                b.Tree(x + side * .93f, y + along, .09f, radius, Color.Lerp(Leaf, C(.30f, .44f, .20f), (float)rng.NextDouble() * .5f));
            }
    }

    // A small square or pocket park: paths, a fountain or kiosk, and trees.
    static void Square(Builder b, float x, float y, bool plaza, System.Random rng)
    {
        b.Box(x, y, .09f, 1.82f, 1.73f, .02f, plaza ? C(.46f, .44f, .38f) : C(.20f, .36f, .24f));
        b.Plate(x, y, .112f, 1.70f, .20f, plaza ? C(.56f, .53f, .46f) : C(.44f, .42f, .36f));
        b.Plate(x, y, .113f, .20f, 1.60f, plaza ? C(.56f, .53f, .46f) : C(.44f, .42f, .36f));
        if (plaza)
        {
            b.Cylinder(x, y, .11f, .30f, .07f, C(.52f, .50f, .44f), 8);
            b.Cylinder(x, y, .18f, .22f, .015f, C(.18f, .44f, .52f), 8);
            b.Cylinder(x, y, .195f, .055f, .12f, C(.60f, .58f, .50f), 6);
        }
        else
            b.Box(x, y, .11f, .30f, .30f, .07f, C(.50f, .46f, .36f));
        for (int t = 0; t < 4; t++)
        {
            float tx = x + (t % 2 == 0 ? -.55f : .55f) + (float)rng.NextDouble() * .16f - .08f;
            float ty = y + (t < 2 ? -.51f : .51f) + (float)rng.NextDouble() * .16f - .08f;
            b.Tree(tx, ty, .11f, .17f + (float)rng.NextDouble() * .06f, Leaf);
        }
    }

    // Surface parking with painted bays and a few cars.
    static void CarPark(Builder b, float x, float y, System.Random rng)
    {
        b.Box(x, y, .09f, 1.85f, 1.8f, .02f, C(.22f, .25f, .27f));
        var carColors = new[] { C(.62f, .20f, .16f), C(.70f, .66f, .58f), C(.18f, .30f, .48f), C(.72f, .60f, .22f), C(.20f, .22f, .24f) };
        for (int lot = 0; lot < 5; lot++)
            for (int side = -1; side <= 1; side += 2)
            {
                float xx = x - .72f + lot * .35f, yy = y + side * .52f;
                b.Plate(xx, yy, .112f, .018f, .43f, C(.62f, .62f, .56f));
                if (rng.NextDouble() < .65)
                    b.Box(xx + .17f, yy, .11f, .15f, .28f, .07f, carColors[rng.Next(carColors.Length)]);
            }
    }

    // A park spanning two blocks and the quiet street between them: lawns, a pond, paths and groves.
    static void Park(Builder b, float x, float y, System.Random rng)
    {
        b.Box(x, y, .03f, 2.05f, 4.48f, .06f, C(.20f, .36f, .22f));
        b.Plate(x, y, .092f, 1.92f, 4.36f, C(.24f, .42f, .25f));
        float pondY = y + (float)(rng.NextDouble() - .5) * 1.4f;
        b.Cylinder(x + .25f, pondY, .09f, .46f, .012f, C(.34f, .42f, .30f), 12);
        b.Cylinder(x + .25f, pondY, .1f, .40f, .01f, C(.16f, .38f, .46f), 12);
        // A path winding the length of the park.
        for (int i = 0; i < 12; i++)
        {
            float v = -2.05f + i * .37f;
            float u = Mathf.Sin(v * 1.6f + 1) * .45f - .2f;
            b.Plate(x + u, y + v, .095f, .14f, .40f, C(.50f, .46f, .38f));
        }
        for (int t = 0; t < 26; t++)
        {
            float tx = x + (float)(rng.NextDouble() - .5) * 1.8f, ty = y + (float)(rng.NextDouble() - .5) * 4.2f;
            if (Mathf.Abs(tx - x - .25f) < .55f && Mathf.Abs(ty - pondY) < .55f)
                continue;
            b.Tree(tx, ty, .09f, .13f + (float)rng.NextDouble() * .09f, Color.Lerp(Leaf, C(.32f, .48f, .22f), (float)rng.NextDouble() * .6f));
        }
        b.Cylinder(x - .55f, y - 1.6f, .09f, .16f, .14f, C(.62f, .58f, .50f), 8);
        b.Cylinder(x - .55f, y - 1.6f, .23f, .19f, .04f, C(.36f, .50f, .46f), 8);
    }

    // A stadium: tiered stands around a striped pitch, with four floodlight masts.
    static void Stadium(Builder b, float x, float y)
    {
        b.Box(x, y, .03f, 2.05f, 4.48f, .06f, Paving);
        float rx = .92f, ry = 1.95f;
        var stand = C(.62f, .62f, .60f);
        int segments = 24;
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2 / segments, c = (i + 1) * Mathf.PI * 2 / segments;
            var p = new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry);
            var q = new Vector2(Mathf.Cos(c) * rx, Mathf.Sin(c) * ry);
            var mid = (p + q) * .5f;
            b.Box(x + mid.x, y + mid.y, .09f, Mathf.Abs(q.x - p.x) + .16f, Mathf.Abs(q.y - p.y) + .16f, .34f, i % 2 == 0 ? stand : stand * .92f);
        }
        b.Box(x, y, .09f, 1.42f, 3.30f, .02f, C(.20f, .44f, .22f));
        for (int stripe = 0; stripe < 6; stripe += 2)
            b.Plate(x, y - 1.375f + stripe * .55f + .275f, .112f, 1.36f, .55f, C(.24f, .50f, .26f));
        var line = C(.85f, .88f, .82f);
        b.Plate(x, y, .113f, 1.30f, .02f, line);
        b.Plate(x - .65f, y, .113f, .02f, 3.20f, line);
        b.Plate(x + .65f, y, .113f, .02f, 3.20f, line);
        b.Plate(x, y - 1.6f, .113f, 1.30f, .02f, line);
        b.Plate(x, y + 1.6f, .113f, 1.30f, .02f, line);
        foreach (int sx in new[] { -1, 1 })
            foreach (int sy in new[] { -1, 1 })
            {
                b.Cylinder(x + sx * .98f, y + sy * 2.12f, .09f, .03f, .9f, C(.50f, .50f, .48f), 5);
                b.Box(x + sx * .98f, y + sy * 2.12f, .99f, .18f, .06f, .04f, C(.92f, .90f, .74f));
            }
    }

    // A rail station: a long canopy over the platforms with tracks running the length of the block.
    static void RailYard(Builder b, float x, float y, System.Random rng)
    {
        b.Box(x, y, .03f, 2.05f, 4.48f, .06f, C(.26f, .25f, .23f));
        var rail = C(.52f, .50f, .46f);
        for (int track = 0; track < 4; track++)
        {
            float tx = x - .72f + track * .48f;
            b.Plate(tx, y, .092f, .26f, 4.40f, C(.20f, .18f, .17f));
            b.Plate(tx - .07f, y, .094f, .018f, 4.40f, rail);
            b.Plate(tx + .07f, y, .094f, .018f, 4.40f, rail);
        }
        for (int platform = 0; platform < 2; platform++)
        {
            float px = x - .48f + platform * .96f;
            b.Box(px, y, .09f, .20f, 3.0f, .05f, C(.56f, .54f, .48f));
            for (int bay = 0; bay < 5; bay++)
                b.Ridge(px, y - 1.2f + bay * .6f, .30f, .42f, .56f, .10f, bay % 2 == 0 ? C(.58f, .32f, .24f) : C(.52f, .29f, .22f));
        }
        // A short train waits at one platform.
        float trainX = x - .72f + rng.Next(4) * .48f;
        for (int car = 0; car < 4; car++)
            b.Box(trainX, y - 1.3f + car * .62f, .10f, .18f, .56f, .12f, car == 0 ? C(.74f, .62f, .22f) : C(.62f, .64f, .66f));
    }
}
