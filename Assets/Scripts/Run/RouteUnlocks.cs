using System.Collections.Generic;

// Both branches have a starting route. A campaign clear charts its sealed exits for the next flight.
public static class RouteUnlocks
{
    static readonly LevelId[] startingDistricts = { LevelId.Fields, LevelId.Snow, LevelId.Thunder, LevelId.City, LevelId.Tunnel };

    public static bool OpenAtStart(LevelId level) => System.Array.IndexOf(startingDistricts, level) >= 0;

    public static IEnumerable<LevelId> OpenedBy(LevelId cleared)
    {
        foreach (var next in LevelRun.GetOptions(cleared))
            if (!OpenAtStart(next))
                yield return next;
    }

    public static string Name(LevelId level) => level == LevelId.Fields ? "Plains" : level.ToString();
    public static string Key(LevelId level) => "Route." + level;
    public static bool IsRoute(string key) => key != null && key.StartsWith("Route.", System.StringComparison.Ordinal);
    // Either late branch can commission a hull when the flight ends there or in Void.
    public static bool CommissionsHull(LevelId final) => LevelRun.Tier(final) >= 5;
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
        ReviewOpensAllRoutes || RouteUnlocks.OpenAtStart(level) || (flight != null && runRoutes != null ? runRoutes : Data.routes).Contains(level.ToString());

    public static bool RouteOpensNextFlight(LevelId level) => !RouteOpen(level) && Data.routes.Contains(level.ToString());

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
            int firstItem = Data.pendingUnlocks.FindIndex(pending => !RouteUnlocks.IsRoute(pending.key));
            Data.pendingUnlocks.Insert(firstItem < 0 ? Data.pendingUnlocks.Count : firstItem, notice);
        }
    }

    /// <summary>Debug: every district opens for the next flight.</summary>
    static void OpenAllRoutes()
    {
        foreach (LevelId level in System.Enum.GetValues(typeof(LevelId)))
            if (!RouteUnlocks.OpenAtStart(level) && !Data.routes.Contains(level.ToString()))
                Data.routes.Add(level.ToString());
    }
}
