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
            default: return false;
        }
        return true;
    }
}
