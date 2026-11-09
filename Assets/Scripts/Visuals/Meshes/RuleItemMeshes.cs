using UnityEngine;

// Pickups for the items that each change one general combat rule.
public static partial class IndustrialMeshFactory
{
    static bool TryRuleItem(Builder b, string kind)
    {
        switch (kind)
        {
            case "Corkscrew": CorkscrewModel(b); break;
            default: return false;
        }
        return true;
    }
}
