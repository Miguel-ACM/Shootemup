using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static Mesh drillLance;

    // A twisted cone along +Y; alternating trim tiles read as a spiral once it spins.
    static void SpiralCone(Builder b, Vector3 baseCenter, float length, float radius, int tileA, int tileB)
    {
        const int sides = 8, rings = 6;
        const float twist = 28 * Mathf.Deg2Rad;
        Vector3 P(int ring, int side)
        {
            float r = radius * (1 - ring / (float)rings);
            float a = side * Mathf.PI * 2 / sides + ring * twist;
            return baseCenter + new Vector3(Mathf.Cos(a) * r, length * ring / rings, Mathf.Sin(a) * r);
        }
        for (int ring = 0; ring < rings; ring++)
            for (int side = 0; side < sides; side++)
            {
                int tile = side % 2 == 0 ? tileA : tileB;
                if (ring == rings - 1)
                    b.Face(tile, P(ring, side), P(ring + 1, side), P(ring, side + 1));
                else
                    b.Face(tile, P(ring, side), P(ring + 1, side), P(ring + 1, side + 1), P(ring, side + 1));
            }
        var cap = new Vector3[sides];
        for (int side = 0; side < sides; side++)
            cap[side] = P(0, side);
        b.Face(6, cap);
    }

    /// <summary>The Drill weapon's in-flight lance: base at the origin, tip at +length.</summary>
    public static Mesh DrillLance(float length, float radius)
    {
        if (drillLance != null)
            return drillLance;
        var b = new Builder();
        SpiralCone(b, Vector3.zero, length, radius, 10, 6);
        b.Can(new Vector3(0, -.04f, 0), radius * 1.15f, .1f, 11, 6);
        return drillLance = b.Finish("Drill lance");
    }

    static bool ReferenceItem(Builder b, string kind)
    {
        switch (kind)
        {
            case "DrillItem":
                // Spiral lance on a collar and grip.
                SpiralCone(b, new Vector3(0, -.08f, 0), .62f, .22f, 10, 6);
                b.Can(new Vector3(0, -.14f, 0), .26f, .12f, 11, 6);
                b.Box(new Vector3(0, -.35f, 0), new Vector3(.2f, .3f, .2f), 5, 11);
                foreach (int side in new[] { -1, 1 })
                    b.Box(new Vector3(side * .2f, -.3f, 0), new Vector3(.08f, .18f, .12f), 6);
                return true;
            case "SpellCard":
                // A paper ofuda: flat because it is paper, with a vermilion border and seal.
                b.Box(Vector3.zero, new Vector3(.5f, .78f, .03f), 15, 1);
                foreach (int side in new[] { -1, 1 })
                {
                    b.Box(new Vector3(0, side * .34f, -.02f), new Vector3(.44f, .045f, .01f), 5, 5);
                    b.Box(new Vector3(side * .2f, 0, -.02f), new Vector3(.045f, .64f, .01f), 5, 5);
                }
                b.Drum(new Vector3(0, .08f, -.025f), .13f, .01f, 5, 12, 5);
                b.Drum(new Vector3(0, .08f, -.032f), .075f, .01f, 15, 12, 1);
                b.Box(new Vector3(0, -.19f, -.025f), new Vector3(.03f, .17f, .01f), 11, 11);
                b.Box(new Vector3(0, -.15f, -.025f), new Vector3(.14f, .025f, .01f), 11, 11);
                b.Box(new Vector3(0, -.24f, -.025f), new Vector3(.1f, .025f, .01f), 11, 11);
                return true;
            case "WarpCore":
                // A glowing crystal held between two capped plates by three struts.
                foreach (int side in new[] { -1, 1 })
                    b.Can(new Vector3(0, side * .38f, 0), .23f, .08f, 6, 11);
                for (int i = 0; i < 3; i++)
                {
                    float a = i * Mathf.PI * 2 / 3 + .4f;
                    var rim = new Vector3(Mathf.Cos(a) * .19f, 0, Mathf.Sin(a) * .19f);
                    EquipmentRod(b, rim + Vector3.down * .34f, rim + Vector3.up * .34f, .025f, 6);
                }
                b.Prism(new[] { new Vector2(0, .32f), new Vector2(.15f, 0), new Vector2(0, -.32f), new Vector2(-.15f, 0) }, .16f, 9, Vector3.zero, 7);
                b.Ball(Vector3.zero, new Vector3(.09f, .2f, .09f), 10);
                b.Drum(new Vector3(0, 0, .12f), .27f, .02f, 7, 12, 7);
                return true;
            case "FillableFlask":
                // Dark green glass round flask, glowing ember liquid, narrow corked neck.
                b.Can(new Vector3(0, -.35f, 0), .25f, .04f, 8, 8);
                b.Can(new Vector3(0, -.17f, 0), .24f, .32f, 3, 10);
                b.Can(new Vector3(0, .06f, 0), .19f, .14f, 8, 8);
                b.Can(new Vector3(0, .2f, 0), .07f, .18f, 8, 8);
                b.Can(new Vector3(0, .31f, 0), .09f, .05f, 6, 13);
                b.Box(new Vector3(-.1f, -.14f, -.24f), new Vector3(.035f, .16f, .01f), 15, 15);
                b.Box(new Vector3(.08f, -.2f, -.24f), new Vector3(.12f, .06f, .01f), 10, 10);
                return true;
            case "Kusabimaru":
                // A katana: long pale blade, gold collar, black guard ring and wrapped grip.
                b.Prism(new[] { new Vector2(-.035f, -.02f), new Vector2(.035f, -.02f), new Vector2(.065f, .5f), new Vector2(.02f, .6f), new Vector2(-.015f, .5f) }, .03f, 15, Vector3.zero, 1);
                b.Box(new Vector3(0, -.03f, 0), new Vector3(.08f, .06f, .06f), 10);
                b.Can(new Vector3(0, -.08f, 0), .17f, .04f, 11, 11);
                b.Box(new Vector3(0, -.3f, 0), new Vector3(.08f, .4f, .08f), 5, 11);
                for (int i = 0; i < 4; i++)
                    b.Box(new Vector3(0, -.15f - i * .09f, -.045f), new Vector3(.06f, .03f, .01f), 11, 11);
                b.Box(new Vector3(0, -.52f, 0), new Vector3(.1f, .05f, .1f), 10);
                return true;
            case "ThrowableShield":
                // A round buckler: steel rim, glowing energy face, a gold boss and rim rivets.
                b.Drum(Vector3.zero, .46f, .1f, 6, 16, 11);
                b.Drum(new Vector3(0, 0, -.06f), .36f, .04f, 9, 16, 9);
                b.Drum(new Vector3(0, 0, -.09f), .12f, .05f, 10, 10, 13);
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI / 4;
                    b.Box(new Vector3(Mathf.Cos(a) * .41f, Mathf.Sin(a) * .41f, -.06f), new Vector3(.045f, .045f, .03f), 1);
                }
                return true;
            case "EarlyRise":
                // A rising sun: a warm disc with alternating long and short rays over a horizon bar.
                b.Drum(new Vector3(0, .05f, 0), .24f, .12f, 10, 16, 3);
                b.Drum(new Vector3(0, .05f, -.065f), .15f, .02f, 13, 16, 10);
                for (int i = 0; i < 12; i++)
                {
                    float a = i * Mathf.PI / 6, inner = .29f, outer = i % 2 == 0 ? .47f : .38f;
                    var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    var n = new Vector2(-d.y, d.x) * .045f;
                    var c = new Vector2(0, .05f);
                    b.Prism(new[] { c + d * inner + n, c + d * outer, c + d * inner - n }, .06f, i % 2 == 0 ? 10 : 3, Vector3.zero);
                }
                b.Box(new Vector3(0, -.3f, .02f), new Vector3(.9f, .07f, .1f), 3, 12);
                return true;
            default:
                return false;
        }
    }
}
