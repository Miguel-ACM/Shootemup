using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static bool TrySecondaryItem(Builder b, string kind)
    {
        switch (kind)
        {
            case "SteelWashers": SteelWashersModel(b); break;
            case "ClockworkSpring": ClockworkSpringModel(b); break;
            default: return false;
        }
        return true;
    }

}
