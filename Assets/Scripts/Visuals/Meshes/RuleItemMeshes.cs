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
}
