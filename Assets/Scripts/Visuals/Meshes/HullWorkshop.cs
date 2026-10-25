using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static Vector2 V(float x, float y) => new Vector2(x, y);
    static void Armor(Builder b, Vector2[] shape, float depth, int paint, float z = 0)
    {
        b.Prism(shape, depth, paint, new Vector3(0, 0, z), 6, 11);
    }
    // A real sloping shoulder, rather than an outline painted on a flat slab.
    static void BevelArmor(Builder b, Vector2[] shape, float depth, int paint, float z = 0, float inset = .13f)
    {
        Vector2 center = Vector2.zero;
        foreach (var p in shape)
            center += p;
        center /= shape.Length;
        float area = 0;
        for (int i = 0; i < shape.Length; i++)
        {
            var p = shape[i];
            var q = shape[(i + 1) % shape.Length];
            area += p.x * q.y - q.x * p.y;
        }
        var rim = new Vector3[shape.Length];
        var cap = new Vector3[shape.Length];
        for (int i = 0; i < shape.Length; i++)
        {
            var p = shape[area > 0 ? shape.Length - 1 - i : i];
            rim[i] = new Vector3(p.x, p.y, z - depth * .08f);
            var q = Vector2.Lerp(p, center, inset);
            cap[i] = new Vector3(q.x, q.y, z - depth * .5f);
        }
        b.Prism(shape, depth * .48f, 11, new Vector3(0, 0, z + depth * .16f), 11, 11);
        b.Face(paint, cap);
        for (int i = 0; i < shape.Length; i++)
        {
            int j = (i + 1) % shape.Length;
            b.Face(6, rim[i], cap[i], cap[j], rim[j]);
        }
    }

    static void Inlet(Builder b, float x, float y, float width, float height, float z, int paint = 6)
    {
        var shape = new[] { V(x - width * .5f, y - height * .3f), V(x - width * .35f, y - height * .5f), V(x + width * .35f, y - height * .5f), V(x + width * .5f, y - height * .3f), V(x + width * .5f, y + height * .5f), V(x - width * .5f, y + height * .5f) };
        BevelArmor(b, shape, .14f, paint, z);
        b.Box(new Vector3(x, y, z - .08f), new Vector3(width * .66f, height * .66f, .025f), 11, 11);
        b.Box(new Vector3(x, y + height * .16f, z - .1f), new Vector3(width * .64f, .035f, .035f), 6);
    }

    static void Ring(Builder b, Vector3 center, float inner, float outer, float depth, int paint, int sides = 16)
    {
        for (int i = 0; i < sides; i++)
        {
            float a = i * Mathf.PI * 2 / sides, c = (i + 1) * Mathf.PI * 2 / sides;
            b.Prism(new[] { V(Mathf.Cos(a) * inner, Mathf.Sin(a) * inner), V(Mathf.Cos(a) * outer, Mathf.Sin(a) * outer), V(Mathf.Cos(c) * outer, Mathf.Sin(c) * outer), V(Mathf.Cos(c) * inner, Mathf.Sin(c) * inner) }, depth, paint, center, 11, 6);
        }
    }

    static void Motor(Builder b, float x, float y, float width = .28f, float length = .7f, int paint = 0)
    {
        BevelArmor(b, new[] { V(x - width * .38f, y - length * .46f), V(x + width * .38f, y - length * .46f), V(x + width * .54f, y + length * .18f), V(x + width * .39f, y + length * .41f), V(x - width * .39f, y + length * .41f), V(x - width * .54f, y + length * .18f) }, .31f, paint, -.025f, .18f);
        // The nozzle is a dark cavity; the separate EnemyThrusters component supplies the flame.
        b.Box(new Vector3(x, y + length * .45f, .015f), new Vector3(width * .83f, .12f, .22f), 6, 11);
        b.Box(new Vector3(x, y + length * .5f, -.025f), new Vector3(width * .58f, .06f, .14f), 11, 11);
        b.Box(new Vector3(x, y - length * .02f, -.19f), new Vector3(width * .53f, length * .32f, .025f), 11, 11);
        b.Box(new Vector3(x, y + length * .02f, -.21f), new Vector3(width * .48f, .035f, .025f), 6);
    }

    static void Glass(Builder b, float x, float y, float w, float h, float z = -.25f, int tile = 9)
    {
        BevelArmor(b, new[] { V(x - w * .5f, y + h * .3f), V(x - w * .34f, y - h * .5f), V(x + w * .34f, y - h * .5f), V(x + w * .5f, y + h * .3f), V(x, y + h * .5f) }, .16f, 11, z, .12f);
        var a = new Vector3(x - w * .34f, y + h * .2f, z - .105f);
        var c = new Vector3(x + w * .34f, y + h * .2f, z - .105f);
        var d = new Vector3(x + w * .23f, y - h * .34f, z - .09f);
        var e = new Vector3(x - w * .23f, y - h * .34f, z - .09f);
        var ridge = new Vector3(x, y + h * .17f, z - .19f);
        b.Face(tile, a, ridge, new Vector3(x, y - h * .34f, z - .13f), e);
        b.Face(tile, ridge, c, d, new Vector3(x, y - h * .34f, z - .13f));
        b.Face(6, a, c, ridge);
    }

    static void Barrel(Builder b, float x, float y, float length = .55f, float width = .12f, float z = -.15f)
    {
        b.Box(new Vector3(x, y, z), new Vector3(width, length, width), 11, 6);
        b.Box(new Vector3(x, y - length * .45f, z), new Vector3(width * 1.5f, .12f, width * 1.5f), 6);
        b.Box(new Vector3(x, y - length * .52f, z - .03f), new Vector3(width * .7f, .025f, width * .7f), 11);
    }

    static void ArcPlate(Builder b, float inner, float outer, float from, float to, float z, float depth, int paint)
    {
        int count = Mathf.Max(2, Mathf.CeilToInt((to - from) / 12));
        var outline = new Vector2[(count + 1) * 2];
        for (int i = 0; i <= count; i++)
        {
            float a = Mathf.Lerp(from, to, i / (float)count) * Mathf.Deg2Rad;
            outline[i] = V(Mathf.Cos(a) * outer, Mathf.Sin(a) * outer);
            outline[outline.Length - 1 - i] = V(Mathf.Cos(a) * inner, Mathf.Sin(a) * inner);
        }
        // One mapped plate keeps the atlas border from repeating as a zebra stripe.
        Armor(b, outline, depth, paint, z);
    }
}
