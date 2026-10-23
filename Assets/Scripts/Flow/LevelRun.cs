using UnityEngine;

public enum LevelId { Fields, Tunnel, Snow, Thunder, City }

public static class LevelRun
{
    public static LevelId Current { get; private set; } = LevelId.Fields;
    public static string CurrentName => Current == LevelId.Fields ? "Plains" : Current.ToString();
    public static int Seed {get;private set;}=17381;
    public static int Stage { get; private set; } = 1;
    public static int Loop => (Stage-1)/3+1;
    public static bool ReturnsToPlains => Tier(Current)==3;
    // Health keeps growing after the speed caps preserve readable patterns.
    public static float EnemyHealthScale => 1+(Stage-1)*.22f;
    public static float EnemyTempoScale => Mathf.Min(1.5f,1+(Loop-1)*.08f);
    public static float EnemyShotSpeedScale => Mathf.Min(1.4f,1+(Loop-1)*.06f);
    public static int Tier(LevelId level)
    {
        switch(level)
        {
            case LevelId.Fields:return 1;
            case LevelId.Snow:case LevelId.City:return 2;
            case LevelId.Thunder:case LevelId.Tunnel:return 3;
            default:throw new System.ArgumentOutOfRangeException(nameof(level));
        }
    }
    // Explicit starts are used by environment/boss previews; new games use Plains.
    public static void Reset(LevelId first = LevelId.Fields)
    { Stage=Tier(first);Current=first;Seed=Random.Range(1,10000000); }
    public static void Advance(LevelId next)
    {
        int expected=ReturnsToPlains ? 1 : Tier(Current)+1;
        if(Tier(next)!=expected)throw new System.InvalidOperationException("Invalid district progression");
        Current=next;Stage++;
    }
    public static void GetChoices(out LevelId left,out LevelId right)
    {
        if(ReturnsToPlains)throw new System.InvalidOperationException("Final districts return directly to Plains");
        left=Current==LevelId.Fields ? LevelId.Snow : LevelId.Thunder;
        right=Current==LevelId.Fields ? LevelId.City : LevelId.Tunnel;
    }
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
