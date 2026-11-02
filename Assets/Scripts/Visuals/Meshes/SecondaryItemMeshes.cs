using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static bool TrySecondaryItem(Builder b, string kind)
    {
        switch (kind)
        {
            case "SteelWashers": SteelWashersModel(b); break;
            case "ClockworkSpring": ClockworkSpringModel(b); break;
            case "RollerBearings": RollerBearingsModel(b); break;
            case "Thermos": ThermosModel(b); break;
            case "HeatSink": HeatSinkModel(b); break;
            case "LuckyPenny": LuckyPennyModel(b); break;
            case "PaperclipBox": PaperclipBoxModel(b); break;
            case "FireflyJar": FireflyJarModel(b); break;
            case "SaltShaker": SaltShakerModel(b); break;
            default: return false;
        }
        return true;
    }

}
