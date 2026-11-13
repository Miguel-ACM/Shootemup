#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Headless, edit-mode checks. No play session, player input or persistent save access.
public static class VerifyHangarCombatPolish
{
    const string Folder = "ArtSource/HangarCombatReview";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly List<string> checks = new List<string>();
    static void Check(bool pass, string message)
    {
        if (!pass) throw new Exception(message);
        checks.Add(message);
    }
    static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);

    public static void Run()
    {
        Guard();
        Directory.CreateDirectory(Folder);
        checks.Clear();
        try
        {
            CheckGallery();
            CheckAccess();
            CheckItems();
            CheckRoutes();
            CheckShader();
            BakeHangarGallery.RefreshDescriptions();
            File.WriteAllLines(Folder + "/Verification.txt", new[] { "PASS", "Headless edit-mode checks; visual/gameplay approval remains separate." }.Concat(checks));
            Debug.Log("PASS Hangar/combat polish: " + checks.Count + " checks");
        }
        catch (Exception error)
        {
            File.WriteAllText(Folder + "/Verification.txt", "FAIL\n" + error);
            throw;
        }
    }

    static void Guard()
    {
        if (!Application.dataPath.Replace('\\', '/').Contains("/Build/ReviewCache/Temp/"))
            throw new Exception("Use an isolated review checkout");
    }

    static void CheckGallery()
    {
        var match = typeof(HangarHub).GetMethod("MatchesGalleryName", BindingFlags.Static | BindingFlags.NonPublic);
        bool Matches(string title, string query) => (bool)match.Invoke(null, new object[] { title, query });
        Check(Matches("Fillable Flask", " FLASK ") && Matches("Overdrive", "over") && Matches("Another arm", "") &&
            !Matches("Overdrive", "flask"), "Gallery name search: case-insensitive partial matches, trimmed spaces, clear and no-result queries.");
        var cameraRoot = new GameObject("Gallery review camera");
        var camera = cameraRoot.AddComponent<UnityEngine.Camera>();
        var root = new GameObject("Gallery review hub");
        var hub = root.AddComponent<HangarHub>();
        try
        {
            Set(hub, "view", camera);
            Set(hub, "baseViewSize", 8f);
            Set(hub, "room", 1);
            Set(hub, "center", Vector3.zero);
            var follow = typeof(HangarHub).GetMethod("FollowDeck", Private);
            foreach (float x in new[] { 22f, 26.86f, 31.72f, 22f })
            {
                follow.Invoke(hub, new object[] { new Vector3(x, 25, 0) });
                float half = camera.orthographicSize * 405f / 720f;
                Check(Mathf.Abs(camera.transform.position.x - 26.86f) < .001f &&
                    camera.transform.position.x - half < 22 && camera.transform.position.x + half > 31.72f,
                    "Gallery keeps all three cases framed at pilot X=" + x);
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(cameraRoot); }
        foreach (ShipFrame frame in Enum.GetValues(typeof(ShipFrame)))
        {
            var hull = ShipFrames.Get(frame);
            var equipment = hull.starters.ToList();
            if (hull.primary != null) equipment.Add(hull.primary + "Item");
            foreach (string key in equipment)
            {
                var mesh = IndustrialMeshFactory.ExpansionItem(key);
                Check(mesh != null && mesh.vertexCount > 0, frame + " starting model: " + key);
            }
        }
    }

    static void CheckAccess()
    {
        foreach (var tape in FlightTrials.All)
        {
            var needed = new HashSet<string>(tape.RequiredItems);
            Check(tape.HasAccess(needed.Contains, hull => hull == tape.frame), tape.title + ": own kit and hull suffice; unrelated items/hulls and simulator blueprints are excluded.");
            Check(!tape.HasAccess(_ => true, _ => false), tape.title + ": missing assigned hull locks the tape.");
            foreach (string missing in needed)
                Check(!tape.HasAccess(key => key != missing, _ => true), tape.title + ": requires " + missing);
        }
        var shift = FlightTrials.Find("Second Shift");
        Check(shift.RequiredItems.Contains("Telekinesis") && shift.RequiredItems.Contains("ScatterCannonItem"), "Later-leg equipment is included in each tape's requirements.");
    }

    static void CheckItems()
    {
        foreach (var entry in new[] { (typeof(FillableFlask), 75f), (typeof(Overdrive), 60f) })
        {
            var root = new GameObject("Active cost review");
            root.SetActive(false);
            var item = (AActiveItem)root.AddComponent(entry.Item1);
            entry.Item1.GetMethod("Awake", Private).Invoke(item, null);
            float efficiency = GameRules.activeEnergyCost;
            try
            {
                GameRules.activeEnergyCost = 1;
                Check(item.EffectivePressCost == entry.Item2 && item.CheckValidPress(entry.Item2 - .01f) == -1 &&
                    item.CheckValidPress(entry.Item2) == 0, entry.Item1.Name + " exact affordability boundary at " + entry.Item2 + " energy.");
                GameRules.activeEnergyCost = .8f;
                Check(Mathf.Approximately(item.EffectivePressCost, entry.Item2 * .8f) &&
                    item.PressWithEnergyEfficiency(entry.Item2 * .8f - .01f) == -1 && item.cost == entry.Item2,
                    entry.Item1.Name + " efficiency applies to affordability and restores the base cost.");
            }
            finally { GameRules.activeEnergyCost = efficiency; UnityEngine.Object.DestroyImmediate(root); }
        }
    }

    static void CheckRoutes()
    {
        var newNames = new HashSet<string> { "Hayloft sentry", "Hedgerow file", "Rooftop shadow", "Service lift couriers", "Rooftop relay",
            "Seabed rising school", "Mooring bell relay", "Observation dish" };
        var parts = WavePartLibrary.Plains.Concat(WavePartLibrary.City).Concat(WavePartLibrary.Underwater).Concat(WavePartLibrary.Space)
            .Where(part => newNames.Contains(part.Name)).ToArray();
        Check(parts.Length == 8, "Eight authored motifs are connected to four district libraries.");
        foreach (var part in parts)
        {
            Vector2 previous = part.Route.Sample(part.Route.TravelParameter(0));
            float distance = 0;
            int resting = 0;
            for (int i = 1; i <= 1000; i++)
            {
                float t = i / 1000f;
                float parameter = part.Route.TravelParameter(t);
                var position = part.Route.Sample(parameter);
                var mirrored = part.Route.Sample(parameter, -1);
                if (!float.IsFinite(position.x) || !float.IsFinite(position.y) || Mathf.Abs(position.x + mirrored.x) > .0001f || Mathf.Abs(position.y - mirrored.y) > .0001f)
                    throw new Exception(part.Name + " invalid or asymmetric route");
                float step = Vector2.Distance(position, previous);
                if (step > .03f) throw new Exception(part.Name + " discontinuous movement");
                if (step < .000001f) resting++;
                distance += step;
                previous = position;
            }
            Check(distance > .5f && resting < 500, part.Name + ": continuous mirrored route, bounded holds and exit.");
            var units = part.Generate(0, 1);
            foreach (var unit in units)
            {
                var track = new WaveGeometry.Track(unit);
                Check(track.Radius >= WaveGeometry.Radius(unit.enemy) + part.Route.TrackingRadius - .0001f &&
                    unit.fireTimes.All(time => time >= .65f && time < unit.flightDuration + unit.time),
                    part.Name + ": scheduled firing and reserved hull/pursuit envelope.");
            }
        }
        var sentry = parts.Single(part => part.Name == "Hayloft sentry").Route;
        Check(sentry.Sample(sentry.TravelParameter(.3f)) == sentry.Sample(sentry.TravelParameter(.6f)), "Arc-length conversion preserves the firing hold.");
        foreach (var name in new[] { "Service lift couriers", "Seabed rising school" })
        {
            var route = parts.Single(part => part.Name == name).Route;
            Check(route.Sample(0).y < 0 && route.Sample(1).y > 1, name + " enters below and exits above the arena.");
        }
        foreach (var part in WavePartLibrary.Space.Where(part => part.Enemy == "OrbitalScout"))
            Check(part.Generate(0, 1).All(unit => unit.fireTimes.Length <= 4), part.Name + " scout score has at most three accents plus one Hard reply.");
    }

    static void CheckShader()
    {
        // CombatVisuals resolves the shipped shader/material by its actual asset reference.
        var material = CombatVisuals.ProjectileMaterial;
        Check(material != null && material.HasProperty("_Opacity"), "Projectile material exposes opacity separately from source pixels and colliders.");
    }

    public static void Build()
    {
        Guard();
        if (!File.Exists(Folder + "/Verification.txt") || !File.ReadAllText(Folder + "/Verification.txt").StartsWith("PASS"))
            throw new Exception("Run headless verification first");
        var output = VerifyCombatRework.Argument("--polish-output", "");
        if (!Path.IsPathRooted(output) || Path.GetExtension(output) != ".exe") throw new Exception("Supply --polish-output with an absolute .exe path");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            target = BuildTarget.StandaloneWindows64, locationPathName = output, options = BuildOptions.None
        });
        File.WriteAllText(Folder + "/Build.txt", report.summary.result + "\nErrors=" + report.summary.totalErrors + "; warnings=" + report.summary.totalWarnings + "\n" + output);
        EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
#endif
