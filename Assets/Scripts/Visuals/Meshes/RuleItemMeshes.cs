using UnityEngine;

// Pickups for the items that each change one general combat rule.
public static partial class IndustrialMeshFactory
{
    static bool TryRuleItem(Builder b, string kind)
    {
        switch (kind)
        {
            case "Corkscrew": CorkscrewModel(b); break;
            case "SlingshotBand": SlingshotBandModel(b); break;
            case "InkPot": InkPotModel(b); break;
            case "Grudge": GrudgeModel(b); break;
            case "BallOfYarn": BallOfYarnModel(b); break;
            case "PaperPlane": PaperPlaneModel(b); break;
            case "GlassMarbles": GlassMarblesModel(b); break;
            case "Hourglass": HourglassModel(b); break;
            case "LoadedDice": LoadedDiceModel(b); break;
            case nameof(Bookmark): BookmarkModel(b); break;
            case nameof(Tripod): TripodModel(b); break;
            case nameof(RelaySwitch): RelaySwitchModel(b); break;
            case nameof(SignalFlare): SignalFlareModel(b); break;
            case nameof(LiveWire): LiveWireModel(b); break;
            default: return false;
        }
        return true;
    }

    static void BookmarkModel(Builder b)
    {
        b.Box(new Vector3(0, -.04f, .08f), new Vector3(.61f, .74f, .19f), 11);
        b.Box(new Vector3(.024f, -.04f, .013f), new Vector3(.49f, .65f, .17f), 1);
        b.Box(new Vector3(-.25f, -.04f, -.05f), new Vector3(.12f, .74f, .18f), 11);
        b.Box(new Vector3(0, -.04f, -.13f), new Vector3(.61f, .74f, .09f), 6, 11);
        b.Prism(new[] { new Vector2(-.09f, .49f), new Vector2(.09f, .49f), new Vector2(.09f, -.12f),
            new Vector2(0, -.045f), new Vector2(-.09f, -.12f) }, .055f, 3, new Vector3(.11f, 0, -.19f));
        for (int y = 0; y < 3; y++)
            b.Box(new Vector3(-.12f, -.15f - y * .07f, -.186f), new Vector3(.12f, .022f, .015f), 9);
    }

    static void TripodModel(Builder b)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            b.Prism(new[] { new Vector2(side * .11f, -.08f), new Vector2(side * .2f, -.06f),
                new Vector2(side * .38f, -.47f), new Vector2(side * .28f, -.47f) }, .105f, 6, new Vector3(0, 0, -.045f), 11);
            b.Box(new Vector3(side * .34f, -.46f, -.04f), new Vector3(.2f, .085f, .18f), 11);
        }
        b.Box(new Vector3(0, -.25f, .14f), new Vector3(.095f, .44f, .11f), 11);
        b.Box(new Vector3(0, -.46f, .18f), new Vector3(.17f, .075f, .23f), 6);
        b.Drum(new Vector3(0, -.065f, 0), .2f, .24f, 6, 8, 11);
        b.Box(new Vector3(0, .1f, -.03f), new Vector3(.43f, .28f, .28f), 6, 11);
        b.Box(new Vector3(0, .31f, -.04f), new Vector3(.13f, .36f, .13f), 11);
        b.Box(new Vector3(0, .49f, -.04f), new Vector3(.18f, .085f, .18f), 6);
        b.Box(new Vector3(0, .1f, -.183f), new Vector3(.145f, .075f, .036f), 9);
    }

    static void RelaySwitchModel(Builder b)
    {
        b.Box(new Vector3(0, -.15f, .035f), new Vector3(.68f, .46f, .27f), 11, 6);
        b.Box(new Vector3(0, -.12f, -.12f), new Vector3(.57f, .35f, .085f), 6, 11);
        for (int side = -1; side <= 1; side += 2)
        {
            b.Box(new Vector3(side * .25f, .15f, -.015f), new Vector3(.13f, .25f, .17f), 11);
            b.Box(new Vector3(side * .25f, .27f, -.014f), new Vector3(.18f, .085f, .2f), 9);
            b.Box(new Vector3(side * .22f, -.2f, -.169f), new Vector3(.06f, .08f, .02f), side < 0 ? 7 : 3);
        }
        b.Drum(new Vector3(0, -.055f, -.17f), .11f, .08f, 9, 8, 11);
        b.Prism(new[] { new Vector2(-.055f, -.01f), new Vector2(.06f, -.04f), new Vector2(.22f, .39f),
            new Vector2(.105f, .41f) }, .12f, 6, new Vector3(0, 0, -.245f), 11);
        b.Box(new Vector3(.16f, .39f, -.243f), new Vector3(.24f, .11f, .17f), 3, 11);
    }

    static void SignalFlareModel(Builder b)
    {
        b.Prism(new[] { new Vector2(-.15f, -.4f), new Vector2(.15f, -.4f), new Vector2(.18f, .32f),
            new Vector2(.1f, .45f), new Vector2(-.1f, .45f), new Vector2(-.18f, .32f) }, .26f, 6, Vector3.zero, 11);
        b.Box(new Vector3(0, -.34f, -.01f), new Vector3(.39f, .14f, .31f), 11, 6);
        b.Box(new Vector3(0, -.17f, -.152f), new Vector3(.23f, .14f, .06f), 1, 11);
        b.Box(new Vector3(0, .28f, -.01f), new Vector3(.36f, .12f, .3f), 11, 6);
        b.Drum(new Vector3(.21f, .4f, -.028f), .095f, .06f, 9, 8, 11);
        b.Drum(new Vector3(.21f, .4f, -.07f), .056f, .06f, 11, 8);
        b.Prism(new[] { new Vector2(-.07f, .42f), new Vector2(.07f, .42f), new Vector2(.055f, .58f),
            new Vector2(-.02f, .65f), new Vector2(-.05f, .56f) }, .13f, 3, Vector3.zero, 10);
    }

    static void LiveWireModel(Builder b)
    {
        b.Prism(new[] { new Vector2(-.34f, -.31f), new Vector2(.27f, -.31f), new Vector2(.39f, -.16f),
            new Vector2(.26f, -.025f), new Vector2(-.16f, -.025f), new Vector2(-.25f, .08f),
            new Vector2(-.16f, .19f), new Vector2(.26f, .19f), new Vector2(.26f, .32f),
            new Vector2(-.22f, .32f), new Vector2(-.41f, .08f), new Vector2(-.22f, -.16f),
            new Vector2(.18f, -.16f), new Vector2(.18f, -.18f), new Vector2(-.34f, -.18f) }, .135f, 6, Vector3.zero, 11);
        b.Box(new Vector3(-.34f, -.245f, 0), new Vector3(.23f, .23f, .24f), 11, 6);
        b.Box(new Vector3(.26f, .255f, 0), new Vector3(.22f, .24f, .25f), 11, 6);
        b.Box(new Vector3(-.48f, -.245f, 0), new Vector3(.12f, .10f, .11f), 3, 11);
        b.Box(new Vector3(.4f, .255f, 0), new Vector3(.12f, .10f, .11f), 9, 11);
        b.Prism(new[] { new Vector2(.13f, .54f), new Vector2(.04f, .34f), new Vector2(-.03f, .43f),
            new Vector2(-.15f, .26f), new Vector2(-.09f, .56f), new Vector2(-.015f, .48f) }, .045f, 9, new Vector3(0, 0, -.16f), 3);
    }
}
