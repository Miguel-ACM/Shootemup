using UnityEngine;

public sealed class HourglassWeaver : DistrictEnemy
{
    protected override void Configure()
    {
        health = 71;
        hullScale = .61f;
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        PointWeapon(Muzzle(0), new Vector2(-1, -.2f));
        PointWeapon(Muzzle(1), new Vector2(1, -.2f));
    }

    protected override void Attack()
    {
        // Both shoulders mark the edges of one open lane. The outside grains release last.
        int grains = Dense(3);
        for (int side = -1; side <= 1; side += 2)
        {
            Vector3 lip = Muzzle(side < 0 ? 0 : 1);
            for (int i = 0; i < grains; i++)
            {
                var axis = new Vector2(side, -.12f - .10f * i);
                Fire<CrescentRound>(lip, axis, 2.7f, .22f, i == 0)
                    ?.Pause(.26f + .18f * i, .48f + .08f * i, CombatRound.Turn.Down, 0, 4.25f);
            }
        }
    }
}

public static partial class IndustrialMeshFactory
{
    static Mesh hourglassWeaverMesh;

    public static Mesh BuildHourglassWeaver()
    {
        if (hourglassWeaverMesh != null) return hourglassWeaverMesh;
        var b = new Builder();
        for (int side = -1; side <= 1; side += 2)
        {
            Vector3 P(float x, float y, float z) => new Vector3(side * x, y, z);
            CutBlade(b, new[] { P(.32f, -.65f, -.09f), P(.67f, .04f, -.04f),
                P(.37f, .70f, -.11f) }, .076f, .07f, 13);
            CutStone(b, P(.62f, .04f, -.08f), new Vector3(.10f, .14f, .07f), 5, 4);
            FieldBarrel(b, P(.33f, -.68f, -.16f), .075f, .19f);
        }
        for (int end = -1; end <= 1; end += 2)
        {
            float y = end * .63f;
            BevelArmor(b, new[] { V(-.49f, y), V(-.33f, y + .11f), V(.31f, y + .12f),
                V(.48f, y), V(.31f, y - .10f), V(-.30f, y - .10f) }, .20f, 13);
            FoldedFin(b, new Vector3(-.30f, end * .53f, -.07f), new Vector3(.30f, end * .53f, -.07f),
                new Vector3(.22f, end * .29f, -.10f), new Vector3(0, end * .035f, -.12f),
                new Vector3(-.10f, end * .34f, -.22f), 4, 15);
        }
        CutBlade(b, new[] { new Vector3(.12f, .24f, -.20f), new Vector3(0, .02f, -.23f),
            new Vector3(-.04f, -.38f, -.20f) }, .044f, .023f, 13);
        b.Prism(new[] { V(-.24f, -.49f), V(-.035f, -.29f), V(.23f, -.49f) },
            .04f, 13, new Vector3(0, 0, -.18f));
        CutStone(b, new Vector3(.02f, .79f, -.06f), new Vector3(.085f, .13f, .055f), 7, 4);
        return hourglassWeaverMesh = b.Finish(nameof(HourglassWeaver));
    }
}

public static partial class WavePartLibrary
{
    public static readonly WavePart[] HourglassWeaverParts =
    {
        DeepPart("Borrowed seconds", nameof(HourglassWeaver), WaveShape.Rank, 2, 6f, 2, new[] { .29f, .57f, .73f },
            -.14f, 1.35f, -.15f, .94f, -.27f, .77f, -.15f, .67f, .37f, .72f, 1.4f, .77f),
        DeepPart("Pendulum crossing", nameof(HourglassWeaver), WaveShape.Train, 3, 6.3f, 5, new[] { .25f, .47f, .70f },
            -1.4f, .89f, -.73f, .82f, -.32f, .56f, .17f, .49f, .61f, .72f, 1.4f, .94f),
        DeepPart("Time runs uphill", nameof(HourglassWeaver), WaveShape.Rake, 2, 5.8f, 8, new[] { .37f, .56f, .73f },
            .55f, -.32f, .62f, .14f, .42f, .43f, .08f, .61f, -.31f, .85f, -.41f, 1.36f),
    };
}
