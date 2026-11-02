#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class VerifyHangarDeck
{
    public const string Folder = "ReviewOutput/HangarDeck";
    const string Key = "HangarDeckReview";
    static double deadline;
    static VerifyHangarDeck()
    {
        EditorApplication.playModeStateChanged += s =>
        {
            if (s != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false))
                return;
            deadline = EditorApplication.timeSinceStartup + 180;
            new GameObject("Dock review").AddComponent<HangarDeckReview>();
        };
        EditorApplication.update += () =>
        {
            if (deadline == 0 || !SessionState.GetBool(Key, false))
                return;
            if (!File.Exists(Folder + "/Result.txt") && EditorApplication.timeSinceStartup < deadline)
                return;
            SessionState.SetBool(Key, false);
            EditorApplication.Exit(File.Exists(Folder + "/Result.txt") && File.ReadAllText(Folder + "/Result.txt") == "PASS" ? 0 : 1);
        };
    }
    static void Guard()
    {
        if (!Application.dataPath.Replace('\\', '/').Contains("/Temp/DecisionHoverReview/"))
            throw new Exception("Use isolated review checkout");
    }
    public static void Lane()
    {
        SessionState.SetBool("InspectDockLane", true);
        Run();
    }
    public static void Polish()
    {
        SessionState.SetBool("InspectDockArt", true);
        Run();
    }
    public static void Run()
    {
        Guard();
        Directory.CreateDirectory(Folder);
        File.Delete(Folder + "/Result.txt");
        PlayerSettings.companyName = "ShootemupReview";
        PlayerSettings.productName = Key + DateTime.UtcNow.Ticks;
        typeof(VerifySecondaryItems).GetMethod("NativeGameView", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        SessionState.SetBool(Key, true);
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Title.unity");
        EditorApplication.isPlaying = true;
    }
    public static void Build()
    {
        Guard();
        if (File.ReadAllText(Folder + "/Result.txt") != "PASS")
            throw new Exception("Review dock first");
        PlayerSettings.companyName = "DefaultCompany";
        PlayerSettings.productName = "seup";
        string output = VerifyCombatRework.Argument("--dock-output", "");
        var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(), output, BuildTarget.StandaloneWindows64, BuildOptions.None);
        Debug.Log("Dock build " + report.summary.result + " errors=" + report.summary.totalErrors + " warnings=" + report.summary.totalWarnings);
        EditorApplication.Exit(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
    }
}
public sealed class HangarDeckReview : MonoBehaviour
{
    HangarHub hub;
    string error;
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Application.runInBackground = true;
        Application.logMessageReceived += OnLog;
    }
    void OnDestroy()
    {
        Application.logMessageReceived -= OnLog;
    }
    void OnLog(string message, string stack, LogType type)
    {
        if ((type == LogType.Error || type == LogType.Exception) && !stack.Contains("UnityEditor.Search"))
            error = message;
    }
    void Check(bool ok, string message)
    {
        Debug.Log("Dock review  " + message);
        if (!ok)
            throw new Exception(message);
    }
    void Call(string name, params object[] args) => typeof(HangarHub).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(hub, args);
    IEnumerator Capture(string title)
    {
        yield return new WaitForSecondsRealtime(.45f);
        yield return new WaitForEndOfFrame();
        var image = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(VerifyHangarDeck.Folder + "/" + title + ".png", image.EncodeToPNG());
        Destroy(image);
    }
    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(1);
        FindAnyObjectByType<NewGame>().OnAccept();
        while ((hub = FindAnyObjectByType<HangarHub>()) == null || CloudTransition.Busy || HangarHub.HoldingShip)
            yield return null;
        yield return new WaitForSecondsRealtime(2);
        if (SessionState.GetBool("InspectDockLane", false))
        {
            SessionState.SetBool("InspectDockLane", false);
            yield return Capture("Concourse");
            Check(error == null, error ?? "final lane lighting");
            File.WriteAllText(VerifyHangarDeck.Folder + "/Result.txt", "PASS");
            yield break;
        }
        if (SessionState.GetBool("InspectDockArt", false))
        {
            SessionState.SetBool("InspectDockArt", false);
            yield return CheckPresentation();
            Check(error == null, error ?? "no gameplay errors");
            File.WriteAllText(VerifyHangarDeck.Folder + "/Result.txt", "PASS");
            yield break;
        }
        Check(MetaProgress.CanFly(ShipFrame.GR07) && !MetaProgress.CanFly(ShipFrame.Lancer), "only starting hull released");
        yield return Capture("Concourse");
        string[] names = { "Fleet", "Armory", "Simulator", "Recorder", "Dispatch" };
        for (int i = 0; i < 5; i++)
        {
            Call("OpenDock", i);
            if (i != 1)
                Call("Navigate", true, 1);
            yield return Capture(names[i]);
            Call("CloseDock");
            yield return null;
        }
        var stock = MetaProgress.Stock;
        Check(stock.Count == 3, "three central offers");
        string pin = stock[0];
        MetaProgress.TogglePin(pin);
        GameRules.StartNewRun();
        LevelRun.Reset(LevelId.Hell);
        MetaProgress.BeginRun();
        MetaProgress.EngageBoss();
        MetaProgress.DefeatBoss();
        RunCoins.Add(81);
        RunCoins.TrySpend(12);
        GameOverState.Show(true);
        yield return new WaitForSecondsRealtime(2);
        yield return Capture("Victory");
        Check(MetaProgress.CanFly(ShipFrame.Lancer) && !MetaProgress.CanFly(ShipFrame.Ember), "one sequential unlock");
        Check(MetaProgress.Coins == 69 && MetaProgress.LastFlight.earned == 81 && MetaProgress.Stock.Contains(pin), "gross, net and held stock");
        Check((MetaProgress.Record(ShipFrame.GR07, RunDifficulty.Normal).flawless & 1 << (int)LevelId.Hell) != 0, "clean boss stamp");
        var old = hub;
        while (hub == old || hub == null || CloudTransition.Busy || HangarHub.HoldingShip)
        {
            hub = FindAnyObjectByType<HangarHub>();
            yield return null;
        }
        Call("OpenDock", 2);
        Call("Navigate", true, 1);
        // Review uses the same launch coroutine as the room's confirmation.
        FlightTrials.Active = FlightTrials.All[1];
        Call("CloseDock");
        hub.StartCoroutine((IEnumerator)typeof(HangarHub).GetMethod("Launch", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(hub, null));
        WaveOrchestrer flow;
        while ((flow = FindAnyObjectByType<WaveOrchestrer>()) == null || flow.Cargo == null)
            yield return null;
        Check(ShipFrames.Current.frame == ShipFrame.Warden && LevelRun.Current == LevelId.Snow && MetaProgress.Ship == ShipFrame.GR07, "loaned Warden and cargo in Snow");
        var ship = FindAnyObjectByType<Shipcontroller>();
        ship.ObserverMode = true;
        typeof(WaveOrchestrer).GetMethod("FinishCargoWave", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(flow, null);
        typeof(WaveOrchestrer).GetMethod("FinishCargoWave", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(flow, null);
        Check(error == null, error ?? "no gameplay errors");
        Check(GameOverState.Active && MetaProgress.ContractClear("Cold Delivery") && MetaProgress.IsUnlocked("Thermos"), "sim objective certifies blueprint");
        File.WriteAllText(VerifyHangarDeck.Folder + "/Result.txt", "PASS");
    }
    IEnumerator CheckPresentation()
    {
        yield return Capture("Concourse");
        for (int i = 0; i < 5; i++)
        {
            Call("OpenDock", i);
            if (i != 1)
                Call("Navigate", true, 1);
            yield return Capture(new[] { "Fleet", "Armory", "Simulator", "Recorder", "Dispatch" }[i]);
            Call("CloseDock");
            yield return null;
        }
        // Exercise the regular departure and terminal routing callback with a post-boss fixture.
        hub.StartCoroutine((IEnumerator)typeof(HangarHub).GetMethod("Launch", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(hub, null));
        WaveOrchestrer flow;
        while ((flow = FindAnyObjectByType<WaveOrchestrer>()) == null || HangarHub.Launching || CloudTransition.Busy)
            yield return null;
        LevelRun.Reset(LevelId.Hell);
        MetaProgress.EnterDistrict();
        MetaProgress.EngageBoss();
        MetaProgress.DefeatBoss();
        RunCoins.Add(183);
        RunCoins.TrySpend(30);
        Score.IncreaseScore(18236);
        flow.StartCoroutine((IEnumerator)typeof(WaveOrchestrer).GetMethod("ChooseNextRoute", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(flow, null));
        while (!GameOverState.Active)
            yield return null;
        yield return new WaitForSecondsRealtime(3);
        yield return Capture("Victory");
        Check(MetaProgress.LastFlight.outcome == "Victory" && MetaProgress.CanFly(ShipFrame.Lancer), "Hell reward exit ends campaign and releases Lancer");
        while (GameOverState.Active || (hub = FindAnyObjectByType<HangarHub>()) == null || CloudTransition.Busy || HangarHub.HoldingShip)
            yield return null;
        Call("OpenDock", 0);
        yield return Capture("StampedHull");
        Call("CloseDock");
        Call("Switch", ShipFrame.Lancer);
        MetaProgress.BeginRun();
        MetaProgress.EngageBoss();
        MetaProgress.TookHit();
        MetaProgress.DefeatBoss();
        GameOverState.Show();
        yield return new WaitForSecondsRealtime(2);
        yield return Capture("GameOver");
        Check(MetaProgress.LastFlight.outcome == "Lost" && MetaProgress.LastFlight.awards.Count == 1 && !MetaProgress.CanFly(ShipFrame.Ember), "loss files a new boss stamp and preserves hull lock");
    }

}
#endif
