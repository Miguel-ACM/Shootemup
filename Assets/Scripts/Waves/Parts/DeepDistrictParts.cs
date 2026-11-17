using UnityEngine;

public static partial class WavePartLibrary
{
    static WavePart DeepPart(string name, string actor, WaveShape shape, int count, float seconds, int unlock, float[] score, params float[] path)
        => new WavePart(name, actor, Route(name, Vector2.right, path), shape, count, seconds, unlock, score);

    public static readonly WavePart[] PlagueMoteParts =
    {
        DeepPart("Drain flock", nameof(PlagueMote), WaveShape.Chevron, 5, 5.3f, 2, System.Array.Empty<float>(),
            -.72f, 1.27f, -.57f, 1.05f, -.28f, .8f, .03f, .51f, .12f, .24f, .23f, -.28f),
        DeepPart("Culvert spill", nameof(PlagueMote), WaveShape.Rake, 5, 5.2f, 3, System.Array.Empty<float>(),
            -1.36f, .7f, -.73f, .69f, -.26f, .62f, .31f, .49f, .72f, .29f, 1.29f, .18f),
        DeepPart("Backwash brood", nameof(PlagueMote), WaveShape.Diamond, 5, 5.6f, 6, System.Array.Empty<float>(),
            -.55f, 1.28f, -.37f, 1.02f, -.16f, .77f, .06f, .48f, .38f, .25f, .65f, -.23f),
        DeepPart("Infested flues", nameof(PlagueMote), WaveShape.Train, 6, 4.8f, 3, System.Array.Empty<float>(),
            -1.38f, .84f, -.81f, .82f, -.49f, .72f, -.32f, .49f, -.12f, .18f, .05f, -.26f).Under(DistrictCondition.Plague),
    };

    public static readonly WavePart[] SiltLeechParts =
    {
        DeepPart("Silting pool", nameof(SiltLeech), WaveShape.Solo, 1, 6.7f, 2, System.Array.Empty<float>(),
            .6f, 1.26f, .54f, .86f, .31f, .66f, .12f, .48f, .31f, .3f, .65f, -.24f),
        DeepPart("Sticky banks", nameof(SiltLeech), WaveShape.Rank, 2, 6.1f, 4, System.Array.Empty<float>(),
            -1.34f, .53f, -.74f, .58f, -.2f, .62f, .25f, .52f, .71f, .44f, 1.34f, .54f),
        DeepPart("Leech eddies", nameof(SiltLeech), WaveShape.Rake, 2, 6.8f, 8, System.Array.Empty<float>(),
            .61f, -.24f, .66f, .2f, .45f, .37f, .2f, .49f, .28f, .75f, .55f, 1.29f),
    };

    public static readonly WavePart[] SluiceBargeParts =
    {
        DeepPart("Open the sluice", nameof(SluiceBarge), WaveShape.Solo, 1, 6f, 2, new[] { .31f, .51f, .71f },
            -.2f, 1.23f, -.28f, .97f, -.35f, .77f, -.25f, .61f, .08f, .52f, .61f, -.22f),
        DeepPart("Twin locks", nameof(SluiceBarge), WaveShape.Train, 2, 6.3f, 5, new[] { .31f, .56f, .73f },
            .43f, 1.31f, .40f, .94f, .28f, .74f, -.13f, .61f, -.63f, .56f, -1.38f, .48f),
        DeepPart("Spillway convoy", nameof(SluiceBarge), WaveShape.Train, 3, 6.3f, 10, new[] { .33f, .62f },
            -1.31f, .91f, -.7f, .89f, -.27f, .79f, .12f, .6f, .49f, .34f, .65f, -.24f),
    };

    public static readonly WavePart[] OverflowBellParts =
    {
        DeepPart("Over the weir", nameof(OverflowBell), WaveShape.Rake, 2, 5.6f, 2, new[] { .29f, .49f, .67f },
            -1.35f, .58f, -.69f, .7f, -.34f, .82f, .3f, .84f, .67f, .73f, 1.34f, .58f),
        DeepPart("Bell tower runoff", nameof(OverflowBell), WaveShape.Train, 3, 5.8f, 6, new[] { .28f, .48f, .68f },
            .66f, 1.29f, .61f, .98f, .38f, .8f, .12f, .77f, -.37f, .81f, -1.36f, .93f),
        DeepPart("Under the arch", nameof(OverflowBell), WaveShape.Rank, 2, 6.4f, 9, new[] { .28f, .54f, .72f },
            .5f, -.26f, .62f, .18f, .55f, .44f, .27f, .67f, -.18f, .74f, -.58f, 1.24f),
    };

    public static readonly WavePart[] PipeSkaterParts =
    {
        DeepPart("Siphon skate", nameof(PipeSkater), WaveShape.Train, 4, 5.2f, 2, new[] { .26f, .47f, .68f },
            -1.37f, .82f, -.7f, .85f, -.08f, .68f, .19f, .46f, .48f, .28f, .76f, -.28f),
        DeepPart("Pipe overtake", nameof(PipeSkater), WaveShape.Chevron, 3, 5.1f, 3, new[] { .28f, .58f },
            .59f, 1.27f, .54f, .94f, .1f, .78f, -.35f, .56f, -.61f, .48f, -1.38f, .4f),
        DeepPart("Drain lip relay", nameof(PipeSkater), WaveShape.Rake, 3, 5.7f, 7, new[] { .25f, .54f, .73f },
            -1.34f, .3f, -.74f, .35f, -.38f, .57f, .12f, .72f, .64f, .64f, 1.37f, .58f),
    };

    public static readonly WavePart[] TransitCubeParts =
    {
        DeepPart("Crossfeed blocks", nameof(TransitCube), WaveShape.Train, 3, 6.3f, 2, System.Array.Empty<float>(),
            -1.38f, .62f, -.88f, .63f, -.42f, .57f, .24f, .42f, .76f, .29f, 1.37f, .25f),
        DeepPart("Diagonal clearance", nameof(TransitCube), WaveShape.Rake, 3, 5.5f, 3, System.Array.Empty<float>(),
            -1.37f, .3f, -.75f, .35f, -.28f, .48f, .25f, .62f, .65f, .76f, 1.38f, .83f),
        DeepPart("Descending pallets", nameof(TransitCube), WaveShape.Rank, 3, 6f, 6, System.Array.Empty<float>(),
            -.51f, 1.27f, -.52f, .91f, -.38f, .69f, -.32f, .47f, -.36f, .19f, -.33f, -.24f),
    };

    public static readonly WavePart[] FuseCubeParts =
    {
        DeepPart("Break the gate", nameof(FuseCube), WaveShape.Rank, 3, 6f, 2, System.Array.Empty<float>(),
            .02f, 1.23f, .12f, .98f, .2f, .81f, .2f, .53f, .18f, .29f, .08f, -.24f),
        DeepPart("Drop shelves", nameof(FuseCube), WaveShape.Rake, 3, 5.8f, 4, System.Array.Empty<float>(),
            -1.35f, .74f, -.62f, .76f, -.15f, .71f, .03f, .53f, .31f, .32f, .64f, -.24f),
        DeepPart("Offset fuse line", nameof(FuseCube), WaveShape.Train, 4, 5.5f, 8, System.Array.Empty<float>(),
            -1.34f, .74f, -.88f, .72f, -.31f, .64f, .31f, .72f, .8f, .79f, 1.35f, .79f),
    };

    public static readonly WavePart[] SentryCubeParts =
    {
        DeepPart("Rail sentries", nameof(SentryCube), WaveShape.Rank, 2, 6.6f, 2, new[] { .23f, .42f, .59f, .76f },
            -1.36f, .72f, -.85f, .72f, -.56f, .76f, -.38f, .71f, -.43f, .55f, -1.36f, .52f),
        DeepPart("Aim ports", nameof(SentryCube), WaveShape.Rake, 3, 6.2f, 5, new[] { .32f, .57f },
            -.63f, 1.28f, -.65f, .96f, -.44f, .77f, -.05f, .65f, .45f, .57f, 1.34f, .47f),
        DeepPart("Rack transfer", nameof(SentryCube), WaveShape.Train, 3, 6.3f, 8, new[] { .25f, .51f, .72f },
            -1.36f, .48f, -.78f, .49f, -.28f, .58f, .29f, .66f, .82f, .66f, 1.36f, .62f),
    };

    public static readonly WavePart[] RamCubeParts =
    {
        DeepPart("Heavy transfer", nameof(RamCube), WaveShape.Solo, 1, 6.7f, 4, new[] { .28f, .56f, .75f },
            -1.37f, .73f, -.8f, .72f, -.38f, .64f, .31f, .59f, .75f, .5f, 1.38f, .51f),
        DeepPart("Cross press", nameof(RamCube), WaveShape.Rank, 2, 6.7f, 7, new[] { .3f, .57f, .73f },
            .46f, 1.29f, .53f, .92f, .45f, .75f, .14f, .64f, -.27f, .61f, -1.37f, .59f),
        DeepPart("Closing jaws", nameof(RamCube), WaveShape.Solo, 1, 6.4f, 10, new[] { .25f, .54f, .71f },
            -1.36f, .56f, -.82f, .54f, -.54f, .47f, -.31f, .42f, -.6f, .39f, -1.36f, .32f),
    };

    public static readonly WavePart[] RelayCarrierParts =
    {
        DeepPart("Red bus", nameof(RelayCarrier), WaveShape.Solo, 1, 6f, 2, new[] { .25f, .42f, .60f, .76f },
            .42f, 1.26f, .44f, .97f, .26f, .75f, -.04f, .67f, -.48f, .69f, -1.36f, .74f),
        DeepPart("Switching chamber", nameof(RelayCarrier), WaveShape.Train, 2, 7f, 5, new[] { .25f, .55f, .73f },
            -1.34f, .87f, -.68f, .85f, -.22f, .72f, .3f, .72f, .73f, .83f, 1.34f, .87f),
        DeepPart("Open breaker", nameof(RelayCarrier), WaveShape.Train, 3, 6.5f, 9, new[] { .3f, .53f, .74f },
            .52f, 1.28f, .49f, .91f, .11f, .75f, -.3f, .57f, -.51f, .39f, -.51f, -.24f),
    };

    public static readonly WavePart[] RivetCutterParts =
    {
        DeepPart("Shunt express", nameof(RivetCutter), WaveShape.Train, 4, 5.2f, 2, new[] { .28f, .49f, .69f },
            -1.36f, .61f, -.68f, .67f, -.22f, .75f, .28f, .71f, .67f, .57f, 1.36f, .5f),
        DeepPart("Return duct", nameof(RivetCutter), WaveShape.Chevron, 3, 5.4f, 4, new[] { .29f, .62f },
            .78f, -.26f, .77f, .22f, .51f, .42f, .28f, .61f, .03f, .73f, -.27f, 1.27f),
        DeepPart("Crossing rivets", nameof(RivetCutter), WaveShape.Rake, 3, 5.2f, 7, new[] { .27f, .54f, .72f },
            -.43f, 1.25f, -.37f, .94f, -.18f, .72f, .16f, .56f, .42f, .25f, .66f, -.24f),
    };
}
