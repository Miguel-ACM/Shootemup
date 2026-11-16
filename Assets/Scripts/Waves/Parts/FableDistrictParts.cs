using UnityEngine;

public static partial class WavePartLibrary
{
    public static readonly WavePart[] SporeBellParts =
    {
        DeepPart("Cap procession", nameof(SporeBell), WaveShape.Train, 3, 6.8f, 2, new[] { .27f, .51f },
            -.69f, 1.26f, -.71f, 1.01f, -.51f, .89f, -.13f, .88f, .29f, .82f, .62f, 1.27f),
        DeepPart("Purple galleries", nameof(SporeBell), WaveShape.Rank, 2, 6.3f, 4, new[] { .28f, .57f },
            -1.37f, .85f, -.74f, .88f, -.29f, .85f, .13f, .79f, .64f, .68f, 1.34f, .61f),
        DeepPart("Crosswind spores", nameof(SporeBell), WaveShape.Rake, 2, 6.6f, 8, new[] { .28f, .47f, .64f },
            .51f, 1.29f, .38f, 1.04f, -.02f, .91f, -.37f, .82f, -.68f, .72f, -1.35f, .67f),
        DeepPart("Canopy drip", nameof(SporeBell), WaveShape.Solo, 1, 7.1f, 6, new[] { .29f, .53f, .71f },
            -.12f, 1.24f, -.28f, .98f, -.42f, .89f, -.19f, .86f, .25f, .85f, .42f, 1.24f),
    };

    public static readonly WavePart[] CapRicochetParts =
    {
        DeepPart("Red cap pinball", nameof(CapRicochet), WaveShape.Train, 4, 5.6f, 2, new[] { .3f, .62f },
            -.73f, 1.29f, -.58f, 1.05f, -.29f, .81f, .17f, .66f, .61f, .37f, 1.33f, .1f),
        DeepPart("From the roots", nameof(CapRicochet), WaveShape.Rake, 3, 6f, 3, new[] { .32f, .64f },
            -1.38f, .57f, -.77f, .61f, -.39f, .58f, .18f, .49f, .67f, .31f, 1.37f, .16f),
        DeepPart("Stem slalom", nameof(CapRicochet), WaveShape.Chevron, 3, 5.8f, 6, new[] { .28f, .58f },
            .62f, 1.3f, .6f, 1.02f, .37f, .76f, -.13f, .71f, -.69f, .58f, -1.38f, .49f),
        DeepPart("Cap relay", nameof(CapRicochet), WaveShape.Train, 4, 6.1f, 10, new[] { .3f, .5f, .71f },
            -1.36f, .91f, -.7f, .84f, -.09f, .76f, .33f, .58f, .69f, .4f, .87f, -.24f),
    };

    public static readonly WavePart[] MyceliumLancerParts =
    {
        DeepPart("White-gill rake", nameof(MyceliumLancer), WaveShape.Rake, 3, 5.8f, 2, new[] { .3f, .61f },
            -1.36f, .74f, -.71f, .81f, -.25f, .75f, .18f, .63f, .64f, .54f, 1.36f, .59f),
        DeepPart("The tangled path", nameof(MyceliumLancer), WaveShape.Train, 4, 6f, 4, new[] { .28f, .52f, .73f },
            -.59f, 1.28f, -.51f, 1.01f, -.33f, .71f, .13f, .53f, .5f, .32f, .57f, -.25f),
        DeepPart("Shaded clearing", nameof(MyceliumLancer), WaveShape.Rank, 2, 6.5f, 6, new[] { .26f, .54f, .73f },
            .46f, 1.28f, .47f, .98f, .28f, .79f, -.31f, .67f, -.61f, .56f, -1.35f, .47f),
        DeepPart("Twin tendrils", nameof(MyceliumLancer), WaveShape.Chevron, 3, 6f, 9, new[] { .3f, .55f, .74f },
            .34f, 1.31f, .13f, 1.03f, -.23f, .78f, -.52f, .67f, -.53f, .36f, -.18f, -.26f),
    };

    public static readonly WavePart[] PaperKiteParts =
    {
        DeepPart("Folded procession", nameof(PaperKite), WaveShape.Train, 4, 5.4f, 2, new[] { .29f, .61f },
            -.66f, 1.33f, -.58f, .98f, -.26f, .79f, .19f, .69f, .66f, .62f, 1.37f, .64f),
        DeepPart("Hall of folds", nameof(PaperKite), WaveShape.Train, 3, 5.7f, 3, new[] { .28f, .59f },
            -1.36f, .53f, -.73f, .54f, -.2f, .65f, .19f, .73f, .67f, .82f, 1.38f, .93f),
        DeepPart("Reverse ceiling", nameof(PaperKite), WaveShape.Rake, 3, 5.7f, 7, new[] { .31f, .61f },
            .66f, -.25f, .74f, .2f, .52f, .44f, .16f, .65f, -.22f, .86f, -.45f, 1.29f),
        DeepPart("Cut-paper wake", nameof(PaperKite), WaveShape.Train, 4, 5.5f, 10, new[] { .26f, .51f, .74f },
            -1.36f, .86f, -.63f, .88f, -.12f, .72f, .19f, .6f, .59f, .36f, .74f, -.26f),
    };

    public static readonly WavePart[] CrescentDancerParts =
    {
        DeepPart("Moonlit arabesque", nameof(CrescentDancer), WaveShape.Rake, 3, 6.2f, 2, new[] { .31f, .59f },
            -.55f, 1.29f, -.48f, 1f, -.18f, .81f, .22f, .66f, .62f, .54f, 1.36f, .58f),
        DeepPart("Sickle promenade", nameof(CrescentDancer), WaveShape.Rank, 2, 6.6f, 4, new[] { .27f, .55f, .74f },
            -1.35f, .53f, -.73f, .57f, -.2f, .69f, .36f, .77f, .72f, .71f, 1.35f, .61f),
        DeepPart("The moon reverses", nameof(CrescentDancer), WaveShape.Train, 3, 6.1f, 6, new[] { .29f, .56f, .73f },
            .51f, -.28f, .64f, .2f, .61f, .44f, .27f, .62f, -.3f, .79f, -.62f, 1.3f),
        DeepPart("Two scenes at once", nameof(CrescentDancer), WaveShape.Chevron, 3, 6.4f, 9, new[] { .25f, .54f, .71f },
            -.25f, 1.25f, -.34f, .95f, -.43f, .77f, -.22f, .57f, .17f, .34f, .4f, -.26f),
    };

    public static readonly WavePart[] PaintedMaskParts =
    {
        DeepPart("Split portrait", nameof(PaintedMask), WaveShape.Solo, 1, 6.7f, 2, new[] { .29f, .54f, .75f },
            -1.35f, .84f, -.69f, .83f, -.18f, .73f, .22f, .61f, .62f, .6f, 1.37f, .71f),
        DeepPart("Passing faces", nameof(PaintedMask), WaveShape.Train, 2, 7.2f, 5, new[] { .26f, .51f, .75f },
            .46f, 1.32f, .37f, .98f, .17f, .8f, -.29f, .71f, -.66f, .66f, -1.35f, .52f),
        DeepPart("Profile and front", nameof(PaintedMask), WaveShape.Solo, 1, 6.8f, 7, new[] { .3f, .6f, .76f },
            -.22f, 1.28f, -.16f, 1.01f, .14f, .78f, .44f, .72f, .59f, .91f, .68f, 1.26f),
        DeepPart("Broken frame", nameof(PaintedMask), WaveShape.Rake, 2, 6.6f, 10, new[] { .28f, .58f, .74f },
            -1.34f, .65f, -.73f, .69f, -.29f, .71f, -.09f, .62f, -.37f, .47f, -1.37f, .41f),
    };
}
