using UnityEngine;

public enum LevelId { Fields, Tunnel, Snow, Thunder, City, Desert, Candyland, Space }

public static class LevelRun
{
    public static LevelId Current { get; private set; } = LevelId.Fields;
    public static string CurrentName => Current == LevelId.Fields ? "Plains" : Current.ToString();
    public static int Seed {get;private set;}=17381;
    public static int Stage { get; private set; } = 1;
    public static int Loop { get; private set; } = 1;
    public static bool ReturnsToPlains => Current==LevelId.Space;
    public static LevelId? AutomaticNext => Current==LevelId.Desert || Current==LevelId.Candyland ? LevelId.Space : ReturnsToPlains ? LevelId.Fields : (LevelId?)null;
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
            case LevelId.Desert:case LevelId.Candyland:return 4;
            case LevelId.Space:return 5;
            default:throw new System.ArgumentOutOfRangeException(nameof(level));
        }
    }
    // Explicit starts are used by environment/boss previews; new games use Plains.
    public static void Reset(LevelId first = LevelId.Fields)
    { Reset(first,Random.Range(1,10000000)); }
    public static void Reset(LevelId first,int seed)
    { Stage=Tier(first);Loop=1;Current=first;Seed=Mathf.Max(1,seed&int.MaxValue); }
    public static void Advance(LevelId next)
    {
        int tier=Tier(Current);
        bool valid=tier==3 ? next==LevelId.Fields || next==LevelId.Desert || next==LevelId.Candyland :
            AutomaticNext.HasValue ? next==AutomaticNext.Value : Tier(next)==tier+1;
        if(!valid)throw new System.InvalidOperationException("Invalid district progression");
        if(next==LevelId.Fields)Loop++;
        Current=next;Stage++;
    }
    public static void GetChoices(out LevelId left,out LevelId right)
    {
        if(ReturnsToPlains)throw new System.InvalidOperationException("Final districts return directly to Plains");
        if(Tier(Current)==4){left=right=LevelId.Space;return;}
        if(Tier(Current)==3){left=LevelId.Desert;right=LevelId.Fields;return;}
        left=Current==LevelId.Fields ? LevelId.Snow : LevelId.Thunder;
        right=Current==LevelId.Fields ? LevelId.City : LevelId.Tunnel;
    }
    public static LevelId[] GetOptions()
    {
        if(Tier(Current)==4)return new[]{LevelId.Space};
        GetChoices(out var left,out var right);
        return Tier(Current)==3 ? new[]{left,LevelId.Candyland,right} : new[]{left,right};
    }
    public static Color Accent(LevelId level)
    {
        switch(level)
        {
            case LevelId.Fields:return new Color(.54f,.77f,.43f);
            case LevelId.Tunnel:return new Color(.95f,.65f,.30f);
            case LevelId.Snow:return new Color(.65f,.88f,.98f);
            case LevelId.Thunder:return new Color(.68f,.60f,.94f);
            case LevelId.Desert:return new Color(.88f,.61f,.29f);
            case LevelId.Candyland:return new Color(.95f,.48f,.72f);
            case LevelId.Space:return new Color(.59f,.69f,.96f);
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
            case LevelId.Desert:return "THE BURIED MACHINES";
            case LevelId.Candyland:return "SUGAR RUSH";
            case LevelId.Space:return "THE SILENT ORBIT";
            default:return "THE CONCRETE CANYONS";
        }
    }
}
