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
}
