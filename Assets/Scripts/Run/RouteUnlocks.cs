using System.Collections.Generic;

// A fresh save flies Plains > Snow > Thunder > Space > Hell. Beating a boss opens the district beside it;
// Snow also opens Tunnel with City so the City branch never reaches two sealed doors.
// New districts open from the next run; the campaign ends at Hell until Void opens.
public static class RouteUnlocks
{
    static readonly LevelId[] firstRoute = { LevelId.Fields, LevelId.Snow, LevelId.Thunder, LevelId.Space, LevelId.Hell };

    public static bool OnFirstRoute(LevelId level) => System.Array.IndexOf(firstRoute, level) >= 0;

    public static IEnumerable<LevelId> OpenedBy(LevelId cleared)
    {
        switch (cleared)
        {
            case LevelId.Snow:
                yield return LevelId.City;
                yield return LevelId.Tunnel;
                break;
            case LevelId.City:
                yield return LevelId.Candyland;
                break;
            case LevelId.Thunder:
                yield return LevelId.Desert;
                break;
            case LevelId.Space:
                yield return LevelId.Underwater;
                break;
            case LevelId.Hell:
                yield return LevelId.Void;
                break;
            case LevelId.Void:
                yield return LevelId.Heaven;
                break;
        }
    }

    public static string Name(LevelId level) => level == LevelId.Fields ? "Plains" : level.ToString();
    public static string Key(LevelId level) => "Route." + level;
    public static bool IsRoute(string key) => key != null && key.StartsWith("Route.", System.StringComparison.Ordinal);
    public static string Requirement(LevelId cleared) => "Beat the " + Name(cleared) + " boss.";
}

public static partial class MetaProgress
{
    // Fixed at departure: a district opened mid-run waits for the next flight.
    static List<string> runRoutes;
#if UNITY_EDITOR
    // Route-flow reviews fly every district without writing anyone's save.
    public static bool ReviewOpensAllRoutes { private get; set; }
#else
    static bool ReviewOpensAllRoutes => false;
#endif

    /// <summary>Whether the campaign may fly into this district. Simulator contracts never choose routes.</summary>
    public static bool RouteOpen(LevelId level) =>
        ReviewOpensAllRoutes || RouteUnlocks.OnFirstRoute(level) || (flight != null && runRoutes != null ? runRoutes : Data.routes).Contains(level.ToString());

    static void AwardRoutes()
    {
        foreach (var level in RouteUnlocks.OpenedBy(LevelRun.Current))
        {
            if (Data.routes.Contains(level.ToString())) continue;
            Data.routes.Add(level.ToString());
            var notice = new ItemUnlockNotice
            {
                key = RouteUnlocks.Key(level), title = RouteUnlocks.Name(level),
                source = RouteUnlocks.Requirement(LevelRun.Current), area = level,
            };
            flight?.unlocks.Add(notice);
            Data.pendingUnlocks.Add(notice);
        }
    }

    /// <summary>Debug: every district opens for the next flight.</summary>
    static void OpenAllRoutes()
    {
        foreach (LevelId level in System.Enum.GetValues(typeof(LevelId)))
            if (!RouteUnlocks.OnFirstRoute(level) && !Data.routes.Contains(level.ToString()))
                Data.routes.Add(level.ToString());
    }
}
