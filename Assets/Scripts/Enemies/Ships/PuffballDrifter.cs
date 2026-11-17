using UnityEngine;

public sealed class PuffballDrifter : DistrictEnemy
{
    protected override void Configure()
    {
        health = 32;
        hullScale = .53f;
    }

    protected override void Attack()
    {
        int pairs = Dense(2);
        Vector3 pore = Muzzle();
        for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < pairs; i++)
            {
                var loft = new Vector2(side * (.54f + .32f * i), .78f - .22f * i);
                Fire<SimpleRound>(pore, loft, 2.05f + .35f * i, .23f, side < 0 && i == 0)
                    ?.Fall(new Vector2(-side * .28f, -2.9f));
            }
    }
}

public static partial class IndustrialMeshFactory
{
    static Mesh puffballDrifterMesh;

    public static Mesh BuildPuffballDrifter()
    {
        if (puffballDrifterMesh != null) return puffballDrifterMesh;
        var b = new Builder();
        // A ripe puffball splitting at its crown, with three papery husk leaves.
        KeelSkin(b, 0, 15, Frame(-.68f, .08f, .06f), Frame(-.39f, .27f, .19f),
            Frame(-.06f, .52f, .28f), Frame(.34f, .56f, .34f), Frame(.60f, .35f, .21f),
            Frame(.70f, .13f, .09f));
        for (int side = -1; side <= 1; side += 2)
        {
            Vector3 P(float x, float y, float z) => new Vector3(side * x, y, z);
            FoldedFin(b, P(.11f, -.41f, -.02f), P(.43f, -.30f, -.13f), P(.73f, .04f, .03f),
                P(.56f, -.47f, .01f), P(.43f, -.41f, -.19f), 8, 14);
            CutBlade(b, new[] { P(.34f, .42f, -.19f), P(.28f, .25f, -.29f), P(.40f, .06f, -.20f) },
                .050f, .025f, 13);
        }
        FoldedFin(b, new Vector3(-.12f, -.43f, 0), new Vector3(.16f, -.47f, -.07f),
            new Vector3(-.23f, -.89f, .02f), new Vector3(-.28f, -.66f, 0), new Vector3(-.09f, -.61f, -.18f), 14, 8);
        FieldPort(b, new Vector3(0, .28f, -.24f), .21f, 13);
        return puffballDrifterMesh = b.Finish(nameof(PuffballDrifter));
    }
}

public static partial class WavePartLibrary
{
    public static readonly WavePart[] PuffballDrifterParts =
    {
        DeepPart("Over the nurse log", nameof(PuffballDrifter), WaveShape.Train, 3, 6.1f, 2, new[] { .32f, .64f },
            -1.4f, .58f, -.72f, .64f, -.33f, .82f, .10f, .85f, .54f, .66f, 1.4f, .58f),
        DeepPart("Updraft in the glade", nameof(PuffballDrifter), WaveShape.Rank, 2, 6.4f, 4, new[] { .30f, .53f, .71f },
            -.62f, 1.33f, -.49f, .91f, -.29f, .58f, .03f, .51f, .39f, .78f, .56f, 1.34f),
        DeepPart("Windfall", nameof(PuffballDrifter), WaveShape.Rake, 3, 5.8f, 7, new[] { .28f, .49f, .67f },
            1.4f, .91f, .66f, .82f, .23f, .59f, -.13f, .58f, -.47f, .72f, -1.4f, .78f),
    };
}
