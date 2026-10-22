using UnityEngine;

public enum LevelId { Fields, Tunnel, Snow, Thunder, City }

public static class LevelRun
{
    public static LevelId Current { get; private set; } = LevelId.Fields;
    public static string CurrentName => Current.ToString();
    public static int Stage { get; private set; } = 1;
    public static void Reset(LevelId first = LevelId.Fields) { Current=first;Stage=1; }
    public static void Advance(LevelId next) { Current=next;Stage++; }
    public static Color Accent(LevelId level)
    {
        switch(level)
        {
            case LevelId.Fields:return new Color(.54f,.77f,.43f);
            case LevelId.Tunnel:return new Color(.95f,.65f,.30f);
            case LevelId.Snow:return new Color(.65f,.88f,.98f);
            case LevelId.Thunder:return new Color(.68f,.60f,.94f);
            default:return new Color(.95f,.76f,.42f);
        }
    }
    public static string Description(LevelId level)
    {
        switch(level)
        {
            case LevelId.Fields:return "THE RIVER VALLEY";
            case LevelId.Tunnel:return "BENEATH THE SURFACE";
            case LevelId.Snow:return "WHITEOUT";
            case LevelId.Thunder:return "INTO THE STORM";
            default:return "THE CONCRETE CANYONS";
        }
    }
}
