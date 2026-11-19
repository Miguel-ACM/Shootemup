using UnityEngine;

// The Void sovereign's four obsidian forms. Each form is a solid central target plus
// separate limbs that pivot at the body origin (VoidAnatomyVisual swings them).
// Layered black plates sit over a glowing violet underlayer, so the seams between
// plates and the silhouette edges read as purple light against the black arena.
// Tiles in VoidBody.shader: 3 glowing vein, 8 living eye, 14 bright plate, 11/6 obsidian.
public static partial class IndustrialMeshFactory
{
    static readonly Mesh[] voidForms = new Mesh[4], voidCores = new Mesh[8];
    static readonly Mesh[,] voidLimbs = new Mesh[8, 4];
    const int VoidVein = 3, VoidIris = 8, VoidPlate = 14, VoidStone = 6, VoidKeel = 11;

    public static Mesh VoidSovereignBossMesh() => VoidForm(0);

    public static Mesh VoidForm(int form)
    {
        if (voidForms[form] != null) return voidForms[form];
        int count = form == 3 ? 4 : 2;
        var parts = new CombineInstance[count + 1];
        parts[0] = new CombineInstance { mesh = VoidCore(form), transform = Matrix4x4.identity };
        for (int i = 0; i < count; i++)
            parts[i + 1] = new CombineInstance { mesh = VoidLimb(form, i), transform = Matrix4x4.identity };
        var mesh = new Mesh { name = "Void / fractured obsidian form " + form };
        mesh.CombineMeshes(parts);
        return voidForms[form] = mesh;
    }

    // Faces turn away from a point inside their solid, so lighting never inverts.
    static void VoidFace(Builder b, int tile, Vector3 inside, params Vector3[] points)
    {
        int count = 0;
        var clean = new Vector3[points.Length];
        foreach (var p in points)
            if (count == 0 || (p - clean[count - 1]).sqrMagnitude > 1e-8f) clean[count++] = p;
        while (count > 1 && (clean[count - 1] - clean[0]).sqrMagnitude <= 1e-8f) count--;
        if (count < 3) return;
        System.Array.Resize(ref clean, count);
        var middle = Vector3.zero;
        foreach (var p in clean) middle += p;
        middle /= count;
        var normal = Vector3.zero;
        for (int i = 0; i < count; i++) normal += Vector3.Cross(clean[i] - middle, clean[(i + 1) % count] - middle);
        if (Vector3.Dot(normal, middle - inside) < 0) System.Array.Reverse(clean);
        b.Face(tile, clean);
    }

    // A faceted blade with a diamond section: two lit bevels meeting at a raised ridge,
    // and a hidden keel behind. Left, ridge and right are given per section, so blades
    // can be asymmetric, hooked or serrated.
    static void VoidRidge(Builder b, Vector3[] left, Vector3[] ridge, Vector3[] right, float lift, int tile, int keelTile = VoidKeel, int rightTile = -1)
    {
        int n = ridge.Length;
        var top = new Vector3[n];
        var under = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            float half = (left[i] - right[i]).magnitude * .5f;
            top[i] = ridge[i] + Vector3.back * lift * half;
            under[i] = ridge[i] + Vector3.forward * lift * half * .6f;
        }
        int other = rightTile < 0 ? tile : rightTile;
        for (int i = 0; i < n - 1; i++)
        {
            int j = i + 1;
            var inside = (ridge[i] + ridge[j]) * .5f;
            VoidFace(b, tile, inside, left[i], top[i], top[j], left[j]);
            VoidFace(b, other, inside, top[i], right[i], right[j], top[j]);
            VoidFace(b, keelTile, inside, right[i], under[i], under[j], right[j]);
            VoidFace(b, keelTile, inside, under[i], left[i], left[j], under[j]);
        }
        foreach (int end in new[] { 0, n - 1 })
        {
            var inside = ridge[end] + (ridge[end == 0 ? 1 : n - 2] - ridge[end]) * .2f;
            VoidFace(b, tile, inside, left[end], top[end], right[end]);
            VoidFace(b, keelTile, inside, left[end], right[end], under[end]);
        }
    }

    // A swept blade along a path. Widths per point; skew slides the ridge toward one edge
    // (-1 left .. 1 right) so the two bevels catch different light.
    static void VoidSweep(Builder b, Vector3[] path, float[] widths, float lift, int tile, float skew = 0, int rightTile = -1)
    {
        int n = path.Length;
        var left = new Vector3[n];
        var right = new Vector3[n];
        var ridge = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            var tangent = (path[Mathf.Min(i + 1, n - 1)] - path[Mathf.Max(0, i - 1)]).normalized;
            var side = new Vector3(-tangent.y, tangent.x, 0).normalized;
            left[i] = path[i] + side * widths[i];
            right[i] = path[i] - side * widths[i];
            ridge[i] = path[i] - side * widths[i] * skew;
        }
        VoidRidge(b, left, ridge, right, lift, tile, VoidKeel, rightTile);
    }

    // A raised faceted plate: an outline fanned up to one apex, with walls and a back.
    static void VoidShard(Builder b, Vector3[] outline, Vector3 apex, float thickness, int tile, int wallTile = VoidStone)
    {
        int n = outline.Length;
        var middle = Vector3.zero;
        foreach (var p in outline) middle += p;
        middle /= n;
        var inside = middle + Vector3.forward * thickness * .5f;
        var back = new Vector3[n];
        for (int i = 0; i < n; i++) back[i] = outline[i] + Vector3.forward * thickness;
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            VoidFace(b, tile, Vector3.Lerp(inside, apex, .3f), apex, outline[i], outline[j]);
            VoidFace(b, wallTile, inside, outline[i], back[i], back[j], outline[j]);
        }
        VoidFace(b, VoidKeel, inside, back);
    }

    static Vector3[] VoidMirror(Vector3[] points, float side)
    {
        var mirrored = new Vector3[points.Length];
        for (int i = 0; i < points.Length; i++) mirrored[i] = new Vector3(points[i].x * side, points[i].y, points[i].z);
        return mirrored;
    }

    // The in-plane perpendicular of a limb path facing away from (sign 1) or toward (-1) the body origin.
    static Vector3 VoidOutward(Vector3 direction, Vector3 point, float sign)
    {
        var normal = new Vector3(-direction.y, direction.x, 0).normalized;
        return Vector3.Dot(normal, new Vector3(point.x, point.y, 0)) * sign >= 0 ? normal : -normal;
    }

    static void VoidEye(Builder b, Vector3 p, Vector3 size)
    {
        CutStone(b, p + Vector3.forward * .035f, Vector3.Scale(size, new Vector3(1.45f, 1.45f, .65f)), 11, 6);
        CutStone(b, p, size, VoidIris, 6);
        CutStone(b, p + Vector3.back * size.z, new Vector3(size.x * .18f, size.y * .85f, .012f), 11, 4);
    }

    public static Mesh VoidCore(int form)
    {
        if (voidCores[form] != null) return voidCores[form];
        var b = new Builder();
        if (form == 0) VoidMaskCore(b);
        else if (form == 1) VoidMantisCore(b);
        else if (form == 2) VoidMawCore(b);
        else if (form == 3) VoidStarCore(b);
        else if (form == 4) VoidMawCore(b);
        else if (form == 5)
        {
            CutStone(b,Vector3.zero,new Vector3(.89f,1.29f,.39f),VoidPlate,8);
            for (int k=0;k<3;k++) CutHalo(b,new Vector3(0,.08f,-.34f-k*.09f),.80f-k*.18f,.07f,Vector3.one,Quaternion.identity,VoidVein,8);
            VoidEye(b,new Vector3(0,.08f,-.68f),new Vector3(.29f,.38f,.12f));
        }
        else if (form == 6) VoidMaskCore(b);
        else VoidStarCore(b);
        return voidCores[form] = b.Finish("Void / carved central target " + form);
    }

    public static Mesh VoidLimb(int form, int index)
    {
        if (voidLimbs[form, index] != null) return voidLimbs[form, index];
        var b = new Builder();
        float side = index == 0 ? -1 : 1;
        if (form == 0) VoidHorn(b, side);
        else if (form == 1) VoidScythe(b, side);
        else if (form == 2) VoidMandible(b, side);
        else if (form == 3) VoidStarPoint(b, index);
        else VoidFinalLimb(b, form-4,index);
        return voidLimbs[form, index] = b.Finish("Void / articulated fracture " + form + " / " + index);
    }

    public static Vector3 VoidFinalAperture(int route, int index)
    {
        float side=(index&1)==0 ? -1 : 1;
        bool upper=index<2;
        return route switch
        {
            0 => new Vector3(side*(upper?1.68f:1.13f),upper?.72f:-1.35f,-.36f),
            1 => new Vector3(side*1.99f,upper?.54f:-.54f,-.30f),
            2 => new Vector3(side*(upper?1.86f:.92f),upper?1.28f:-1.67f,-.40f),
            _ => new Vector3(side*(upper?1.17f:1.87f),upper?1.66f:-.54f,-.32f)
        };
    }

    static void VoidFinalLimb(Builder b,int route,int index)
    {
        Vector3 tip=VoidFinalAperture(route,index);
        float sign=Mathf.Sign(tip.x);
        Vector3 root=new Vector3(sign*.32f,tip.y*.16f,0);
        if(route==0)
        {
            // The carapace opens onto four hooked, inward-turning talons.
            VoidSweep(b,new[]{root,new Vector3(sign*1.48f,tip.y*.38f,-.05f),tip+new Vector3(sign*.36f,.33f,0),tip},
                new[]{.23f,.39f,.21f,.025f},.73f,VoidPlate);
        }
        else if(route==1)
        {
            var elbow=new Vector3(sign*.94f,tip.y,0);
            VoidSweep(b,new[]{root,elbow,tip},new[]{.31f,.28f,.18f},.62f,VoidStone);
            for(int j=0;j<4;j++) b.Box(Vector3.Lerp(elbow,tip,j/4f)+Vector3.back*.17f,new Vector3(.105f,.54f,.15f),VoidPlate,VoidVein);
        }
        else
        {
            // The mask trails asymmetric quills; the solar crown opens pleated gold wings.
            var bend=new Vector3(tip.x*.80f,tip.y*(route==2?.34f:.82f),-.15f);
            VoidSweep(b,new[]{root,bend,tip},new[]{.22f,route==2?.29f:.43f,.035f},.75f,VoidPlate);
            for(int j=0;j<3;j++)
            {
                var seat=Vector3.Lerp(root,bend,.35f+j*.24f);
                var barb=seat+new Vector3(sign*(.45f+j*.09f),route==2?-.52f:.61f,-.11f);
                VoidSweep(b,new[]{seat,barb},new[]{.13f,.012f},.72f,j==1?VoidVein:VoidPlate);
            }
        }
        VoidEye(b,tip,new Vector3(.145f,.17f,.075f));
    }

    // Form 0: a horned mask. Brow, cheek and chin plates part around two living eyes.
    static void VoidMaskCore(Builder b)
    {
        var glow = new[] { new Vector3(0, 1.02f, .08f), new Vector3(.62f, .80f, .08f), new Vector3(.84f, .18f, .08f), new Vector3(.66f, -.52f, .08f), new Vector3(.24f, -1.30f, .08f), new Vector3(0, -1.52f, .08f), new Vector3(-.24f, -1.30f, .08f), new Vector3(-.66f, -.52f, .08f), new Vector3(-.84f, .18f, .08f), new Vector3(-.62f, .80f, .08f) };
        VoidShard(b, glow, new Vector3(0, -.15f, -.12f), .1f, VoidVein, VoidVein);
        for (int s = -1; s <= 1; s += 2)
        {
            // Brow: a heavy overhang that slopes down toward the eye.
            var brow = new[] { new Vector3(.05f, .98f, 0), new Vector3(.58f, .76f, 0), new Vector3(.80f, .20f, 0), new Vector3(.55f, -.02f, 0), new Vector3(.22f, .02f, 0), new Vector3(.05f, .14f, 0) };
            VoidShard(b, VoidMirror(brow, s), new Vector3(s * .36f, .44f, -.44f), .16f, VoidPlate);
            // Cheek: hollow under the eye, tapering to the chin.
            var cheek = new[] { new Vector3(.08f, -.36f, 0), new Vector3(.50f, -.34f, 0), new Vector3(.72f, -.40f, 0), new Vector3(.58f, -.82f, 0), new Vector3(.22f, -1.38f, 0), new Vector3(.03f, -1.50f, 0) };
            VoidShard(b, VoidMirror(cheek, s), new Vector3(s * .40f, -.60f, -.38f), .16f, VoidStone);
            // Temple fang hanging past the cheek.
            VoidSweep(b, new[] { new Vector3(s * .74f, .02f, -.08f), new Vector3(s * .86f, -.40f, -.12f), new Vector3(s * .70f, -.88f, -.10f) }, new[] { .09f, .07f, 0 }, 1.1f, VoidPlate, s * .4f);
            // Crown spikes rising from the brow.
            VoidSweep(b, new[] { new Vector3(s * .30f, .82f, -.12f), new Vector3(s * .36f, 1.16f, -.12f), new Vector3(s * .30f, 1.42f, -.06f) }, new[] { .09f, .06f, 0 }, 1.1f, VoidStone, -s * .3f);
            VoidEye(b, new Vector3(s * .32f, -.18f, -.44f), new Vector3(.21f, .10f, .055f));
        }
        // Central crest and the long nasal keel splitting the face.
        VoidSweep(b, new[] { new Vector3(0, 1.62f, -.10f), new Vector3(0, 1.10f, -.30f), new Vector3(0, .40f, -.48f), new Vector3(0, -.30f, -.42f), new Vector3(0, -1.48f, -.22f) }, new[] { 0, .11f, .10f, .07f, 0 }, 1.6f, VoidPlate, .25f, VoidStone);
        // A vertical mouth slit low on the chin.
        VoidEye(b, new Vector3(0, -.86f, -.44f), new Vector3(.05f, .20f, .035f));
    }

    // Form 0 limbs: one massive sweeping horn per side, banded like layered obsidian,
    // with a backswept spur below it.
    static void VoidHorn(Builder b, float s)
    {
        Vector3 P(float x, float y, float z = 0) => new Vector3(s * x, y, z);
        var horn = new[] { P(.42f, .52f, .04f), P(1.02f, .88f), P(1.58f, 1.40f, -.04f), P(1.78f, 1.98f, -.06f), P(1.52f, 2.42f, -.04f), P(1.12f, 2.52f, 0) };
        VoidSweep(b, horn, new[] { .34f, .32f, .27f, .19f, .10f, 0 }, .9f, VoidPlate, s * .35f, VoidStone);
        // Violet light glows along the inner edge of the horn.
        var under = new Vector3[horn.Length];
        for (int i = 0; i < horn.Length; i++) under[i] = horn[i] + Vector3.forward * .12f;
        VoidSweep(b, under, new[] { .39f, .38f, .32f, .23f, .13f, 0 }, .2f, VoidVein);
        // Glowing growth rings across the horn.
        for (int band = 0; band < 3; band++)
        {
            var c = Vector3.Lerp(horn[band + 1], horn[band + 2], .15f);
            var t = (horn[band + 2] - horn[band]).normalized;
            var n = new Vector3(-t.y, t.x, 0);
            float w = .33f - band * .07f;
            VoidSweep(b, new[] { c + n * w + Vector3.back * .10f, c + Vector3.back * .27f, c - n * w + Vector3.back * .10f }, new[] { .035f, .05f, .035f }, .6f, VoidVein);
        }
        var spur = new[] { P(.62f, .12f, .06f), P(1.18f, .10f), P(1.72f, .36f, -.02f), P(2.18f, .88f, 0) };
        VoidSweep(b, spur, new[] { .23f, .20f, .13f, 0 }, 1.0f, VoidStone, -s * .3f, VoidPlate);
        var spurGlow = new Vector3[spur.Length];
        for (int i = 0; i < spur.Length; i++) spurGlow[i] = spur[i] + Vector3.forward * .1f;
        VoidSweep(b, spurGlow, new[] { .27f, .24f, .16f, 0 }, .2f, VoidVein);
    }

    // Form 1: a mantis. Triangular head, armoured thorax, folded wings and a segmented abdomen.
    static void VoidMantisCore(Builder b)
    {
        // Wings fanned open behind the body in a threat display.
        for (int side = -1; side <= 1; side += 2)
            for (int k = 0; k < 2; k++)
            {
                float angle = (20 + k * 22) * Mathf.Deg2Rad, length = 2.10f - k * .22f;
                var dir = new Vector3(side * Mathf.Sin(angle), -Mathf.Cos(angle), 0);
                var bulge = new Vector3(-dir.y, dir.x, 0) * side * .10f;
                var root = new Vector3(side * .16f, .10f - k * .06f, .22f - k * .03f);
                var vane = new[] { root, root + dir * length * .42f + bulge, root + dir * length * .80f + bulge * .6f, root + dir * length };
                VoidSweep(b, vane, new[] { .07f, .20f, .14f, 0 }, .7f, k == 1 ? VoidPlate : VoidStone, side * .35f, VoidStone);
                var glow = new Vector3[vane.Length];
                for (int i = 0; i < vane.Length; i++) glow[i] = vane[i] + Vector3.forward * .1f;
                VoidSweep(b, glow, new[] { .09f, .25f, .18f, 0 }, .2f, VoidVein);
            }
        // Segmented abdomen: overlapping chevrons narrowing to a sting.
        for (int seg = 0; seg < 5; seg++)
        {
            float y = -.22f - seg * .27f, w = .40f - seg * .065f;
            var plate = new[] { new Vector3(-w, y + .10f, -.04f), new Vector3(0, y + .20f, -.04f), new Vector3(w, y + .10f, -.04f), new Vector3(w * .72f, y - .18f, -.04f), new Vector3(0, y - .30f, -.04f), new Vector3(-w * .72f, y - .18f, -.04f) };
            VoidShard(b, plate, new Vector3(0, y - .04f, -.34f + seg * .03f), .18f, seg % 2 == 0 ? VoidPlate : VoidStone);
        }
        var spine = new[] { new Vector3(0, .0f, .06f), new Vector3(0, -1.66f, .06f) };
        VoidSweep(b, spine, new[] { .30f, .05f }, .2f, VoidVein);
        VoidSweep(b, new[] { new Vector3(0, -1.48f, -.16f), new Vector3(0, -1.78f, -.12f), new Vector3(0, -2.02f, -.04f) }, new[] { .08f, .05f, 0 }, 1.2f, VoidPlate);
        // Thorax: a narrow armoured waist between head and abdomen.
        var thorax = new[] { new Vector3(-.30f, .36f, 0), new Vector3(0, .48f, 0), new Vector3(.30f, .36f, 0), new Vector3(.20f, -.10f, 0), new Vector3(0, -.22f, 0), new Vector3(-.20f, -.10f, 0) };
        VoidShard(b, thorax, new Vector3(0, .14f, -.40f), .2f, VoidStone);
        // Head: a broad inverted triangle with horned corners and a single living eye.
        var head = new[] { new Vector3(-.62f, 1.04f, 0), new Vector3(0, 1.00f, 0), new Vector3(.62f, 1.04f, 0), new Vector3(.20f, .48f, 0), new Vector3(0, .36f, 0), new Vector3(-.20f, .48f, 0) };
        VoidShard(b, head, new Vector3(0, .80f, -.46f), .2f, VoidPlate);
        for (int s = -1; s <= 1; s += 2)
        {
            VoidSweep(b, new[] { new Vector3(s * .50f, .98f, -.10f), new Vector3(s * .70f, 1.30f, -.08f), new Vector3(s * .58f, 1.62f, -.04f) }, new[] { .08f, .05f, 0 }, 1.1f, VoidStone, s * .3f);
            CutStone(b, new Vector3(s * .40f, .88f, -.30f), new Vector3(.10f, .07f, .05f), VoidVein, 6);
        }
        VoidEye(b, new Vector3(0, .66f, -.50f), new Vector3(.14f, .20f, .06f));
    }

    // Form 1 limbs: raptorial arms. A spined femur rises to the elbow, and a broad
    // hooked scythe falls from it with a serrated inner edge and an eye near the hook.
    static void VoidScythe(Builder b, float s)
    {
        Vector3 P(float x, float y, float z = 0) => new Vector3(s * x, y, z);
        var femur = new[] { P(.22f, .30f, .02f), P(.70f, .92f, -.02f), P(1.14f, 1.50f, -.04f), P(1.26f, 1.64f, -.04f) };
        VoidSweep(b, femur, new[] { .13f, .18f, .17f, .13f }, 1.0f, VoidPlate, -s * .3f, VoidStone);
        for (int spine = 0; spine < 3; spine++)
        {
            var root = Vector3.Lerp(femur[0], femur[2], .3f + spine * .25f);
            // Spines rake from the underside of the femur.
            var along = (femur[2] - femur[0]).normalized;
            var below = new Vector3(-along.y, along.x, 0);
            if (below.y > 0) below = -below;
            root += below * .14f + Vector3.back * .06f;
            VoidSweep(b, new[] { root, root + below * .18f - along * .06f }, new[] { .05f, 0 }, 1.0f, VoidStone);
        }
        CutStone(b, P(1.28f, 1.62f, -.12f), new Vector3(.18f, .16f, .12f), VoidStone, 6);
        CutStone(b, P(1.28f, 1.62f, -.22f), new Vector3(.08f, .07f, .04f), VoidVein, 6);
        // The scythe folds back down from the elbow to a hooked point; its inner edge is toothed.
        var blade = new[] { P(1.32f, 1.62f, -.06f), P(1.56f, 1.10f, -.06f), P(1.70f, .40f, -.06f), P(1.68f, -.30f, -.06f), P(1.52f, -.86f, -.04f), P(1.28f, -1.22f, 0) };
        var widths = new[] { .17f, .25f, .28f, .22f, .12f, 0 };
        var under = new Vector3[blade.Length];
        for (int i = 0; i < blade.Length; i++) under[i] = blade[i] + Vector3.forward * .1f;
        VoidSweep(b, under, new[] { .21f, .30f, .34f, .27f, .16f, 0 }, .2f, VoidVein);
        VoidSweep(b, blade, widths, .9f, VoidPlate, s * .45f, VoidStone);
        for (int tooth = 0; tooth < 5; tooth++)
        {
            float t = .10f + tooth * .17f;
            int i = Mathf.Min((int)(t * 5), 4);
            var c = Vector3.Lerp(blade[i], blade[i + 1], t * 5 - i);
            var dir = (blade[i + 1] - blade[i]).normalized;
            var inward = VoidOutward(dir, c, -1);
            float w = Mathf.Lerp(widths[i], widths[i + 1], t * 5 - i);
            var root = c + inward * w * .75f + Vector3.back * .04f;
            VoidSweep(b, new[] { root, root + inward * .18f + dir * .07f }, new[] { .05f, 0 }, 1.0f, VoidStone);
        }
        VoidEye(b, P(1.66f, .40f, -.30f), new Vector3(.11f, .16f, .05f));
    }

    // Form 2: an octagonal maw. Eight armour plates with glowing seams ring a deep
    // violet throat, and two rows of fangs close toward the central eye.
    static void VoidMawCore(Builder b)
    {
        var throat = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            float a = (i + .5f) * Mathf.PI / 4;
            throat[i] = new Vector3(Mathf.Cos(a) * 1.10f, Mathf.Sin(a) * 1.18f, .24f);
        }
        VoidShard(b, throat, new Vector3(0, 0, .02f), .08f, VoidVein, VoidVein);
        for (int i = 0; i < 8; i++)
        {
            float a0 = (i + .5f) * Mathf.PI / 4 + .05f, a1 = (i + 1.5f) * Mathf.PI / 4 - .05f, mid = (i + 1) * Mathf.PI / 4;
            Vector3 R(float a, float r, float z = 0) => new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r * 1.08f, z);
            var plate = new[] { R(a0, .70f), R(a0, 1.08f), R(mid, 1.20f), R(a1, 1.08f), R(a1, .70f) };
            VoidShard(b, plate, R(mid, .96f, -.40f), .22f, i % 2 == 0 ? VoidPlate : VoidStone);
            // Outer fang rooted in the plate, inner fang in the gap between plates.
            VoidSweep(b, new[] { R(mid, .78f, -.22f), R(mid + .05f, .52f, -.18f), R(mid + .10f, .30f, -.10f) }, new[] { .11f, .07f, 0 }, 1.2f, VoidPlate, .3f, VoidStone);
            float gap = (i + .5f) * Mathf.PI / 4;
            VoidSweep(b, new[] { R(gap, .72f, -.06f), R(gap - .06f, .56f, -.04f), R(gap - .10f, .42f, 0) }, new[] { .07f, .04f, 0 }, 1.2f, VoidStone);
            // Rim spikes break the round outline into a sawblade crown.
            VoidSweep(b, new[] { R(mid, 1.08f, -.12f), R(mid, 1.42f, -.08f) }, new[] { .10f, 0 }, 1.0f, VoidStone, .4f, VoidPlate);
        }
        VoidEye(b, new Vector3(0, 0, .02f), new Vector3(.20f, .30f, .055f));
    }

    // Form 2 limbs: crescent mandibles that close around the maw, fanged on the
    // inside and spined on the outside, each carrying an eye near the lower hook.
    static void VoidMandible(Builder b, float s)
    {
        Vector3 P(float x, float y, float z = 0) => new Vector3(s * x, y, z);
        var jaw = new[] { P(.60f, 1.28f, .06f), P(1.26f, 1.42f, .02f), P(1.78f, .94f), P(1.96f, .12f), P(1.72f, -.68f), P(1.22f, -1.24f, .02f), P(.56f, -1.52f, .06f) };
        var widths = new[] { .12f, .25f, .32f, .34f, .30f, .21f, 0 };
        var under = new Vector3[jaw.Length];
        for (int i = 0; i < jaw.Length; i++) under[i] = jaw[i] + Vector3.forward * .12f;
        VoidSweep(b, under, new[] { .16f, .31f, .40f, .42f, .38f, .27f, 0 }, .2f, VoidVein);
        VoidSweep(b, jaw, widths, .9f, VoidStone, s * .3f, VoidPlate);
        for (int i = 1; i < jaw.Length - 1; i++)
        {
            var dir = (jaw[i + 1] - jaw[i - 1]).normalized;
            var outward = VoidOutward(dir, jaw[i], 1);
            // Inner fangs point at the maw, outer spines rake backward.
            var inner = jaw[i] - outward * widths[i] * .85f;
            VoidSweep(b, new[] { inner + Vector3.back * .04f, inner - outward * .36f + dir * .08f + Vector3.back * .06f }, new[] { .08f, 0 }, 1.2f, VoidPlate);
            if (i % 2 == 1)
            {
                var outer = jaw[i] + outward * widths[i] * .85f;
                VoidSweep(b, new[] { outer, outer + outward * .22f - dir * .12f }, new[] { .08f, 0 }, 1.1f, VoidStone);
            }
        }
        VoidEye(b, P(1.36f, -.96f, -.34f), new Vector3(.12f, .14f, .06f));
    }

    // Form 3: a four-pointed star. A diamond heart split by one tall eye, with four
    // short diagonal blades between the long points.
    static void VoidStarCore(Builder b)
    {
        var glow = new[] { new Vector3(0, 1.18f, .10f), new Vector3(.66f, 0, .10f), new Vector3(0, -1.18f, .10f), new Vector3(-.66f, 0, .10f) };
        VoidShard(b, glow, new Vector3(0, 0, -.10f), .08f, VoidVein, VoidVein);
        for (int s = -1; s <= 1; s += 2)
        {
            var half = new[] { new Vector3(s * .06f, 1.02f, 0), new Vector3(s * .58f, 0, 0), new Vector3(s * .06f, -1.02f, 0), new Vector3(s * .26f, 0, 0) };
            VoidShard(b, half, new Vector3(s * .30f, 0, -.46f), .22f, s < 0 ? VoidPlate : VoidStone);
        }
        for (int d = 0; d < 4; d++)
        {
            float a = Mathf.PI * .25f + d * Mathf.PI * .5f;
            var u = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
            VoidSweep(b, new[] { u * .40f + Vector3.forward * .04f, u * .82f, u * 1.28f + Vector3.forward * .04f }, new[] { .17f, .12f, 0 }, 1.1f, d % 2 == 0 ? VoidPlate : VoidStone, .3f, VoidStone);
            VoidSweep(b, new[] { u * .40f + Vector3.forward * .14f, u * 1.38f + Vector3.forward * .14f }, new[] { .21f, 0 }, .2f, VoidVein);
        }
        VoidEye(b, new Vector3(0, 0, -.56f), new Vector3(.18f, .58f, .10f));
    }

    // Form 3 limbs: long crystal points with a glowing seam, side barbs and an eye.
    static void VoidStarPoint(Builder b, int index)
    {
        float a = index == 0 ? Mathf.PI : index == 1 ? 0 : index == 2 ? Mathf.PI * .5f : -Mathf.PI * .5f;
        var u = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
        var v = new Vector3(-u.y, u.x, 0);
        var path = new[] { u * .40f, u * 1.05f + Vector3.back * .04f, u * 1.75f + Vector3.back * .04f, u * 2.36f };
        var widths = new[] { .40f, .32f, .21f, 0 };
        VoidSweep(b, path, widths, 1.0f, VoidPlate, 0, VoidStone);
        // A violet seam of light runs along the crystal's ridge.
        var seam = new Vector3[3];
        for (int i = 0; i < 3; i++) seam[i] = path[i] + Vector3.back * (widths[i] + .012f);
        seam[2] = Vector3.Lerp(seam[1], path[3] + Vector3.back * .012f, .75f);
        VoidSweep(b, seam, new[] { .035f, .03f, 0 }, .5f, VoidVein);
        var under = new Vector3[path.Length];
        for (int i = 0; i < path.Length; i++) under[i] = path[i] + Vector3.forward * .12f;
        VoidSweep(b, under, new[] { .46f, .38f, .26f, 0 }, .2f, VoidVein);
        for (int s = -1; s <= 1; s += 2)
        {
            var root = u * 1.02f + v * s * .28f;
            VoidSweep(b, new[] { root + Vector3.back * .02f, root + v * s * .26f - u * .12f, root + v * s * .42f - u * .34f + Vector3.forward * .04f }, new[] { .09f, .06f, 0 }, 1.1f, VoidStone, 0, VoidPlate);
        }
        VoidEye(b, u * 1.65f + Vector3.back * .30f, new Vector3(.11f, .11f, .05f));
    }
}
