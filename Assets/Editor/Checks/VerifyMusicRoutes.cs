#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Command-line regression of the actual spatial selector and covered campaign transition.
[InitializeOnLoad]
public static class VerifyMusicRoutes
{
    public const string Folder = "C:/Users/Miguel/Shootemup/Build/MusicRouteReview";
    const string Key = "VerifyMusicRoutes";
    static double deadline;
    static VerifyMusicRoutes()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
            deadline = EditorApplication.timeSinceStartup + 240;
            new GameObject("Campaign route regression").AddComponent<MusicRouteProbe>();
        };
        EditorApplication.update += () =>
        {
            if (deadline == 0 || EditorApplication.timeSinceStartup < deadline) return;
            File.WriteAllText(Folder + "/Routes.txt", "FAIL timeout");
            EditorApplication.Exit(1);
        };
    }
    public static void Run()
    {
        if (!Application.isBatchMode || !Application.dataPath.Replace('\\', '/').Contains("/Temp/DecisionHoverReview/"))
            throw new InvalidOperationException("Use the isolated command-line review project.");
        Directory.CreateDirectory(Folder);
        PlayerSettings.companyName = "ShootemupReview";
        PlayerSettings.productName = "MusicRouteReview";
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Key, true);
        EditorApplication.isPlaying = true;
    }

    public static void Build()
    {
        if (!Application.isBatchMode || !Application.dataPath.Replace('\\', '/').Contains("/Temp/DecisionHoverReview/"))
            throw new InvalidOperationException("Use the isolated command-line review project.");
        if (!File.ReadAllText(Folder + "/Routes.txt").StartsWith("PASS\n"))
            throw new InvalidOperationException("Complete the campaign route regression first.");
        foreach (var track in new[] { "heaven", "hell", "void" })
        {
            var importer = AssetImporter.GetAtPath("Assets/Resources/Music/" + track + ".mp3") as AudioImporter;
            if (importer == null || !importer.loadInBackground || Resources.Load<TextAsset>("MusicAnalysis/" + track) == null)
                throw new InvalidOperationException("Missing music cues or background loading for " + track);
        }
        PlayerSettings.companyName = "DefaultCompany";
        PlayerSettings.productName = "seup";
        string output = Folder + "/Player/Shootemup.exe";
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            target = BuildTarget.StandaloneWindows64,
            locationPathName = output
        });
        bool success = result.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
        File.WriteAllText(Folder + "/Build.txt", (success ? "PASS\n" : "FAIL\n") + output +
            "\nErrors=" + result.summary.totalErrors + "; warnings=" + result.summary.totalWarnings + "\n");
        EditorApplication.Exit(success ? 0 : 1);
    }
}

public sealed class MusicRouteProbe : MonoBehaviour
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    readonly List<string> checks = new List<string>();
    WaveOrchestrer flow;
    Shipcontroller ship;
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Application.runInBackground = true;
        Application.targetFrameRate = 60;
        UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = null;
        QualitySettings.renderPipeline = null;
    }
    void Check(bool valid, string message)
    {
        checks.Add((valid ? "PASS " : "FAIL ") + message);
        File.WriteAllLines(VerifyMusicRoutes.Folder + "/Progress.txt", checks);
        if (!valid) throw new Exception(message);
    }
    IEnumerator Start()
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(Run());
        Exception error = null;
        while (stack.Count > 0)
        {
            bool more = false;
            object step = null;
            try { more = stack.Peek().MoveNext(); if (more) step = stack.Peek().Current; }
            catch (Exception exception) { error = exception; }
            if (error != null) break;
            if (!more) { stack.Pop(); continue; }
            if (step is IEnumerator nested) { stack.Push(nested); continue; }
            yield return step;
        }
        File.WriteAllText(VerifyMusicRoutes.Folder + "/Routes.txt", (error == null ? "PASS\n" : "FAIL\n") + string.Join("\n", checks) + "\n" + error);
        EditorApplication.Exit(error == null ? 0 : 1);
    }
    IEnumerator Ready()
    {
        while (CloudTransition.Busy || !WavePreparation.Ready) yield return null;
    }
    IEnumerator Run()
    {
        GameRules.StartNewRun();
        LevelRun.Reset(LevelId.Space, 17381);
        yield return SceneManager.LoadSceneAsync("L1");
        while ((flow = FindAnyObjectByType<WaveOrchestrer>()) == null) yield return null;
        yield return null;
        Time.timeScale = 1;
        flow.enabled = false;
        flow.StopAllCoroutines();
        flow.CancelInvoke();
        ship = FindAnyObjectByType<Shipcontroller>();
        ship.ObserverMode = true;
        ship.enabled = false;
        foreach (var weapon in ship.GetComponentsInChildren<AWeapon>()) { weapon.StopFiring(); weapon.enabled = false; }
        foreach (var collider in ship.GetComponentsInChildren<Collider2D>()) collider.enabled = false;
        yield return Ready();
        foreach (var from in new[] { LevelId.Space, LevelId.Underwater })
            foreach (var target in new[] { LevelId.Heaven, LevelId.Hell })
            {
                foreach (var shop in FindObjectsByType<DecisionShop>()) Destroy(shop);
                flow.StopAllCoroutines();
                flow.CancelInvoke();
                LevelRun.Reset(from, 17381);
                LevelEnvironment.Apply();
                yield return Ready();
                ship.transform.position = new Vector3(0, -4, -1);
                flow.StartCoroutine((IEnumerator)typeof(WaveOrchestrer).GetMethod("ChooseNextRoute", Hidden).Invoke(flow, null));
                LevelChoice choice;
                while ((choice = FindAnyObjectByType<LevelChoice>()) == null) yield return null;
                Check(choice.Options.SequenceEqual(new[] { LevelId.Hell, LevelId.Heaven }), from + " labels match destinations");
                ship.transform.position = new Vector3(target == LevelId.Heaven ? Camera.RightLimit * .7f : Camera.LeftLimit * .7f, -4, -1);
                while (!CloudTransition.Busy) yield return null;
                yield return Ready();
                Check(LevelRun.Current == target, from + " selection arrives in " + target + " (actual " + LevelRun.Current + ")");
                Check(LevelRun.Seed == 17381 && LevelRun.Stage == 5, "transition preserves run seed and tier");
                Check((FindAnyObjectByType<HeavenBackdrop>() != null) == (target == LevelId.Heaven), target + " activates its own scenery");
                Check((FindAnyObjectByType<InfernalCaldera>() != null) == (target == LevelId.Hell), target + " does not activate the other route's scenery");
                Check(DistrictWaves.BossType == (target == LevelId.Heaven ? typeof(SeraphJudicatorBoss) : typeof(HellCrabBoss)), target + " owns its boss");
                Check(WaveComposer.Create(2).units.All(unit => WatcherCatalog.Enemies(target).Any(type => type.Name == unit.enemy)), target + " owns its opening enemies");
                var score = FindAnyObjectByType<DistrictScore>();
                Check(score != null && score.TrackName == BGMManager.LevelTrack(target), target + " selects its own music");
                foreach (var shop in FindObjectsByType<DecisionShop>()) Destroy(shop);
                flow.StopAllCoroutines();
                flow.StartCoroutine((IEnumerator)typeof(WaveOrchestrer).GetMethod("ChooseNextRoute", Hidden).Invoke(flow, null));
                while (!CloudTransition.Busy) yield return null;
                yield return Ready();
                Check(LevelRun.Current == LevelId.Void && LevelRun.FinalDistrict && DistrictWaves.Sequence().SequenceEqual(new[] { "boss" }), target + " continues to the boss-only Void");
                Check(score.TrackName == "void", "Void selects its own music");
            }
    }
}
#endif
