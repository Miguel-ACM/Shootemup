using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static Mesh depthMine;

    // A moored contact mine after Miguel's reference (15 November 2026): a faceted steel sphere
    // with a raised, riveted equatorial seam, contact horns standing on bolted flange rings over the
    // upper hemisphere and around the rim, a crown horn facing the camera, and its mooring chain
    // trailing down into the depths below it. Seen from above, the horns ring the silhouette.
    public static Mesh DepthMineMesh()
    {
        if (depthMine != null) return depthMine;
        var b = new Builder();
        const float Radius = .44f;
        // The sphere: a twice-subdivided icosahedron, the camera-facing cap lighter than the depths.
        var points = IcosphereDirections(2, out var triangles);
        for (int i = 0; i < triangles.Count; i += 3)
        {
            var p0 = points[triangles[i]] * Radius;
            var p1 = points[triangles[i + 1]] * Radius;
            var p2 = points[triangles[i + 2]] * Radius;
            var centre = (p0 + p1 + p2) / 3;
            if (Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), centre) < 0)
                (p1, p2) = (p2, p1);
            int tile = centre.z < -.05f ? 14 : 8;
            b.Face(tile, p0, p1, p2);
        }
        // The equatorial seam: a raised band around the rim with a row of rivets on its face.
        const int Around = 24;
        for (int i = 0; i < Around; i++)
        {
            float a = i * Mathf.PI * 2 / Around, c = (i + 1) * Mathf.PI * 2 / Around;
            Vector3 Rim(float angle, float r, float z) => new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, z);
            b.Face(6, Rim(a, Radius * .99f, -.035f), Rim(c, Radius * .99f, -.035f), Rim(c, Radius + .035f, -.035f), Rim(a, Radius + .035f, -.035f));
            b.Face(6, Rim(a, Radius + .035f, -.035f), Rim(c, Radius + .035f, -.035f), Rim(c, Radius + .035f, .035f), Rim(a, Radius + .035f, .035f));
            if (i % 2 == 0)
                b.Ball(Rim(a + Mathf.PI / Around, Radius + .03f, -.04f), Vector3.one * .016f, 15);
        }
        // Contact horns on bolted flanges: a crown horn, a ring over the upper hemisphere,
        // a ring around the rim and a few under it.
        MineHorn(b, new Vector3(0, 0, -1), Radius, 1.15f);
        for (int i = 0; i < 6; i++)
            MineHorn(b, Polar(i * 60 + 30, 42), Radius, 1);
        for (int i = 0; i < 8; i++)
            MineHorn(b, Polar(i * 45, 82), Radius, 1);
        for (int i = 0; i < 4; i++)
            MineHorn(b, Polar(i * 90 + 45, 122), Radius, .8f);
        // The mooring chain, trailing down the screen into the depths: alternating links, each
        // one darker the deeper it hangs.
        var down = new Vector3(-.2f, -.95f, 1).normalized;
        var anchor = new Vector3(0, 0, Radius * .9f);
        b.Drum(anchor + down * .03f, .07f, .06f, 6, 8);
        for (int link = 0; link < 8; link++)
        {
            var centre = anchor + down * (.12f + link * .13f);
            bool flat = link % 2 == 0;
            var across = flat ? Vector3.Cross(down, Vector3.forward).normalized : Vector3.Cross(down, Vector3.Cross(down, Vector3.forward)).normalized;
            int tile = link < 3 ? 6 : link < 6 ? 8 : 11;
            var top = centre - down * .07f;
            var bottom = centre + down * .07f;
            EquipmentRod(b, top + across * .04f, bottom + across * .04f, .014f, tile);
            EquipmentRod(b, top - across * .04f, bottom - across * .04f, .014f, tile);
            EquipmentRod(b, top - across * .045f, top + across * .045f, .014f, tile);
            EquipmentRod(b, bottom - across * .045f, bottom + across * .045f, .014f, tile);
        }
        return depthMine = b.Finish("Moored contact mine with flanged horns and chain");
    }

    // A direction at an azimuth around the screen and a polar angle from the camera-facing crown.
    static Vector3 Polar(float azimuth, float polar)
    {
        float a = azimuth * Mathf.Deg2Rad, p = polar * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(p) * Mathf.Cos(a), Mathf.Sin(p) * Mathf.Sin(a), -Mathf.Cos(p));
    }

    // One contact horn: a bolted flange ring on the hull, a stalk and a rounded tip.
    static void MineHorn(Builder b, Vector3 direction, float radius, float scale)
    {
        direction.Normalize();
        var foot = direction * radius * .97f;
        EquipmentRod(b, foot, foot + direction * .03f * scale, .075f * scale, 6);
        var across = Vector3.Cross(direction, Mathf.Abs(direction.z) < .9f ? Vector3.forward : Vector3.up).normalized;
        var other = Vector3.Cross(direction, across);
        for (int bolt = 0; bolt < 4; bolt++)
        {
            float a = bolt * Mathf.PI * .5f + Mathf.PI * .25f;
            b.Ball(foot + direction * .035f * scale + (across * Mathf.Cos(a) + other * Mathf.Sin(a)) * .058f * scale, Vector3.one * .011f * scale, 15);
        }
        var tip = foot + direction * .2f * scale;
        EquipmentRod(b, foot + direction * .02f * scale, tip, .034f * scale, 14);
        b.Ball(tip, Vector3.one * .04f * scale, 15);
    }
}
