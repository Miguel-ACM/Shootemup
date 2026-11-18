using UnityEngine;

public sealed class BracketFan : DistrictEnemy
{
    protected override void Configure()
    {
        health = 54;
        hullScale = .53f;
    }

    static int GillSide(int beat) => (beat & 1) == 0 ? -1 : 1;

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        if (!IsPreparing) return;
        int side = GillSide(volley + 1);
        var gill = Muzzle(side < 0 ? 0 : 1);
        Cue(gill, seconds);
        PointWeapon(gill, CombatVisuals.Rotate(Vector2.down, side * 34));
    }

    protected override void Attack()
    {
        int side = GillSide(volley);
        Vector3 gill = Muzzle(side < 0 ? 0 : 1);
        int count = Dense(6);
        for (int i = 0; i < count; i++)
        {
            float turn = side * Mathf.Lerp(16, 53, i / (float)Mathf.Max(1, count - 1));
            Fire<NeedleRound>(gill, CombatVisuals.Rotate(Vector2.down, turn), 2.6f + .22f * i, .18f, i == 0);
        }
    }
}

public static partial class IndustrialMeshFactory
{
    static Mesh bracketFanMesh;

    public static Mesh BuildBracketFan()
    {
        if (bracketFanMesh != null) return bracketFanMesh;
        var b = new Builder();
        // Two overlapping turkey-tail shelves grow from opposite sides of one woody spine.
        KeelSkin(b, .03f, 14, Frame(-.69f, .10f, .07f), Frame(-.38f, .19f, .13f),
            Frame(.22f, .14f, .11f, -.06f), Frame(.75f, .08f, .08f, -.24f));
        void Shelf(Vector3 root, float side, float span, int middle)
        {
            Vector3 Point(int i, float r)
            {
                float a = (-57 + i * 17) * Mathf.Deg2Rad;
                float scallop = (i & 1) == 0 ? 1f : .94f;
                return root + new Vector3(side * Mathf.Cos(a) * r * span * scallop,
                    Mathf.Sin(a) * r * span * .85f, -.16f * (1 - r * r) + .025f * (i & 1));
            }
            for (int i = 0; i < 7; i++)
            {
                int left = side > 0 ? i + 1 : i, right = side > 0 ? i : i + 1;
                b.Face(13, root + Vector3.back * .16f, Point(left, .46f), Point(right, .46f));
                b.Face(middle, Point(right, .46f), Point(left, .46f), Point(left, .83f), Point(right, .83f));
                b.Face(15, Point(right, .83f), Point(left, .83f), Point(left, 1), Point(right, 1));
                b.Face(14, Point(right, 1), Point(left, 1), Point(left, 1) + Vector3.forward * .09f,
                    Point(right, 1) + Vector3.forward * .09f);
            }
        }
        Shelf(new Vector3(-.11f, .28f, -.03f), 1, 1.05f, 5);
        Shelf(new Vector3(.01f, -.10f, -.10f), -1, .69f, 2);
        FieldBarrel(b, new Vector3(-.18f, -.60f, -.16f), .095f, .28f);
        FieldBarrel(b, new Vector3(.20f, -.49f, -.17f), .095f, .31f);
        return bracketFanMesh = b.Finish(nameof(BracketFan));
    }
}

public static partial class WavePartLibrary
{
    public static readonly WavePart[] BracketFanParts =
    {
        DeepPart("Bark switchback", nameof(BracketFan), WaveShape.Train, 3, 4.76f, 2, new[] { .24f, .46f, .68f },
            -.78f, 1.32f, -.53f, .89f, .13f, .76f, .46f, .63f, .27f, .42f, -.44f, -.28f),
        DeepPart("Shelves on the bank", nameof(BracketFan), WaveShape.Rake, 3, 5.08f, 5, new[] { .31f, .56f },
            -1.4f, .48f, -.72f, .51f, -.32f, .62f, .12f, .80f, .64f, .83f, 1.4f, .82f),
        DeepPart("Around the trunk", nameof(BracketFan), WaveShape.Train, 3, 5.25f, 8, new[] { .24f, .47f, .70f },
            .23f, 1.40f, .28f, .96f, .45f, .75f, .30f, .54f, -.27f, .43f, -1.48f, .44f),
    };
}
