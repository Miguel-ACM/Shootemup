#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

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
    public static void Door()
    {
        SessionState.SetBool("InspectPressureDoor",true);
        Run();
    }
    public static void Rooms()
    {
        SessionState.SetBool("InspectPhysicalRooms", true);
        Run();
    }
    public static void Lane()
    {
        SessionState.SetBool("InspectDockLane", true);
        Run();
    }
    public static void Endings()
    {
        SessionState.SetBool("InspectFlightEndings", true);
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
        if(SessionState.GetBool("InspectPhysicalRooms",false) || SessionState.GetBool("InspectPressureDoor",false))
        {
            while(!HangarShutter.Active)yield return null;
            yield return Capture("TitlePressureDoor");
        }
        while ((hub = FindAnyObjectByType<HangarHub>()) == null || CloudTransition.Busy || HangarHub.HoldingShip || HangarShutter.Active)
            yield return null;
        yield return new WaitForSecondsRealtime(2);
        if(SessionState.GetBool("InspectPressureDoor",false))
        {
            SessionState.SetBool("InspectPressureDoor",false);
            yield return Capture("PhysicalLane");
            Check(error==null,error ?? "final pressure door and outfitter lettering");
            File.WriteAllText(VerifyHangarDeck.Folder + "/Result.txt","PASS");
            yield break;
        }
        if (SessionState.GetBool("InspectPhysicalRooms", false))
        {
            SessionState.SetBool("InspectPhysicalRooms", false);
            yield return PhysicalRooms();
            Check(error == null, error ?? "no gameplay errors in connected rooms");
            File.WriteAllText(VerifyHangarDeck.Folder + "/Result.txt", "PASS");
            yield break;
        }
        if (SessionState.GetBool("InspectDockLane", false))
        {
            SessionState.SetBool("InspectDockLane", false);
            yield return TraverseLane();
            Check(error == null, error ?? "scrolling lane and campaign handoff");
            File.WriteAllText(VerifyHangarDeck.Folder + "/Result.txt", "PASS");
            yield break;
        }
        if (SessionState.GetBool("InspectDockArt", false) || SessionState.GetBool("InspectFlightEndings", false))
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
    IEnumerator TraverseLane()
    {
        var keys = InputSystem.AddDevice<Keyboard>();
        void Press(params Key[] state)
        {
            InputSystem.QueueStateEvent(keys, new KeyboardState(state));
            InputSystem.Update();
        }
        var ship = FindAnyObjectByType<Shipcontroller>().transform;
        var view = UnityEngine.Camera.main.transform;
        float cameraOrigin = view.position.y, spawn = ship.position.y;
        yield return Capture("Concourse");
        Press(Key.UpArrow);
        yield return null;
        Check(Mathf.Abs(ship.position.y - spawn - GameRules.playerMoveSpeed * 1.2f * Time.deltaTime) < .05f, "ordinary hangar speed");
        float before = ship.position.y;
        Press(Key.UpArrow, Key.LeftShift);
        yield return null;
        Check(Mathf.Abs(ship.position.y - before - GameRules.playerMoveSpeed * 2.4f * Time.deltaTime) < .05f, "Focus doubles hangar travel speed");
        float deadline = Time.realtimeSinceStartup + 8;
        while (ship.position.y < 16 && Time.realtimeSinceStartup < deadline) yield return null;
        Check(ship.position.y >= 16, "passed the first workshops");
        Press();
        yield return new WaitForSeconds(.45f);
        Check(view.position.y > cameraOrigin + 14 && Mathf.Abs(ship.position.y - view.position.y) < .3f, "camera follows beyond combat ceiling");
        yield return Capture("MidConcourse");
        Press(Key.DownArrow, Key.LeftShift);
        deadline = Time.realtimeSinceStartup + 8;
        while (ship.position.y > 5 && Time.realtimeSinceStartup < deadline) yield return null;
        Check(ship.position.y <= 5, "returned to lower deck");
        Press();
        yield return new WaitForSeconds(.55f);
        Check(Mathf.Abs(ship.position.y - view.position.y) < .3f, "camera follows back down the lane");
        Press(Key.UpArrow, Key.LeftShift);
        deadline = Time.realtimeSinceStartup + 10;
        float gate = Camera.UpLimit + HangarHub.DeckExtension - .1f;
        while (ship.position.y < gate && Time.realtimeSinceStartup < deadline) yield return null;
        Check(ship.position.y >= gate, "flew the full length of the deck");
        Press();
        yield return new WaitForSeconds(.5f);
        Check(!CloudTransition.Busy && Mathf.Abs(view.position.y - cameraOrigin - HangarHub.DeckExtension) < .3f, "gate reachable and camera stops at far end");
        yield return Capture("DepartureLane");
        Press(Key.Z);
        while (HangarHub.Active || HangarHub.Launching || CloudTransition.Busy) yield return null;
        Press();
        Check(Mathf.Abs(view.position.y - cameraOrigin) < .05f && Mathf.Abs(ship.position.y - spawn) < .1f, "launch restores combat camera and ship position");
        InputSystem.RemoveDevice(keys);
    }

    IEnumerator PhysicalRooms()
    {
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        void Press(params Key[] keys) { InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); InputSystem.Update(); }
        var ship = FindAnyObjectByType<Shipcontroller>().transform;
        var camera = UnityEngine.Camera.main.transform;
        var origin = camera.position;
        float deadline;
        yield return Capture("PhysicalLane");
        var coinReader = GameObject.Find("Coin reader");
        Check(coinReader==null || !coinReader.activeSelf, "wallet hidden away from outfitter");
        ship.position = new Vector3(2.7f,0,ship.position.z);
        yield return Capture("GroupedShop");
        Check(FindObjectsByType<Canvas>().Count(c=>c.renderMode==RenderMode.ScreenSpaceOverlay && c.name.Contains("shop"))==0,"shop labels are mounted in the world");
        var overlays=FindObjectsByType<RawImage>().Where(i=>i.name=="Ships").ToArray();
        Check(overlays.All(i=>!i.enabled),"ship overlay stays behind side guards in hangar");
        ship.position = new Vector3(0,12,ship.position.z);
        yield return new WaitForSeconds(.7f);
        Press(Key.LeftArrow);
        deadline=Time.realtimeSinceStartup+5;
        while(ship.position.x > -14 && Time.realtimeSinceStartup<deadline) yield return null;
        Press();
        yield return new WaitForSeconds(.7f);
        Check(ship.position.x < -13 && camera.position.x < -13,"fly through lit left portal into fleet room");
        yield return Capture("FleetRoom");
        Check(FindObjectsByType<Canvas>().Any(c=>c.name=="FLEET mounted screen" && c.renderMode==RenderMode.WorldSpace),"fleet giant screen is mounted inside room");
        ship.position = new Vector3(-16.15f,9.4f,ship.position.z);
        yield return Capture("FleetPad");
        ship.position = new Vector3(-11.85f,9.4f,ship.position.z);
        Press(Key.Z);yield return null;Press();
        Check(MetaProgress.Ship==ShipFrame.GR07,"locked Lancer cannot be selected at its pad");
        ship.position = new Vector3(-14,12,ship.position.z);
        Press(Key.RightArrow);
        deadline=Time.realtimeSinceStartup+7;
        while(ship.position.x < 14 && Time.realtimeSinceStartup<deadline)yield return null;
        Press();
        yield return new WaitForSeconds(.8f);
        Check(ship.position.x>13 && camera.position.x>13,"cross hallway into opposite armory");
        ship.position = new Vector3(14,10.75f,ship.position.z);
        Press(Key.Z);yield return null;Press();
        yield return Capture("ArmoryRoom");
        Check(HangarHub.HoldingShip,"armory console receives controls only at physical desk");
        Press(Key.RightArrow);yield return null;Press();yield return Capture("ArmoryCatalog");
        Press(Key.X);yield return null;Press();
        Check(!HangarHub.HoldingShip,"release console to fly freely inside armory");
        for(int index=2;index<5;index++)
        {
            ship.position = new Vector3(index==2?-14:14,index<4?30:48,ship.position.z);
            yield return new WaitForSeconds(.9f);
            yield return Capture(new[]{"SimulatorRoom","RecorderRoom","FlightRoom"}[index-2]);
        }
        ship.position=new Vector3(-14,28.75f,ship.position.z);
        yield return new WaitForSeconds(.8f);
        Press(Key.Z); yield return null; Press();
        Press(Key.RightArrow); yield return null; Press();
        Press(Key.Z);
        yield return null;
        Press();
        Check(HangarShutter.Active,"simulator closes a pressure door before changing space");
        yield return Capture("SimulatorPressureDoor");
        deadline=Time.realtimeSinceStartup+25;
        while((HangarHub.Active || HangarHub.Launching || CloudTransition.Busy || HangarShutter.Active) && Time.realtimeSinceStartup<deadline)yield return null;
        Check(!HangarHub.Active && !HangarHub.Launching && Mathf.Abs(camera.position.x-origin.x)<.05f && Mathf.Abs(camera.position.y-origin.y)<.05f,"simulator launch restores combat camera");
        Check(FlightTrials.Active==FlightTrials.All[1] && LevelRun.Current==LevelId.Snow && MetaProgress.Ship==ShipFrame.GR07,"physical simulator console launches selected loaned flight");
        InputSystem.RemoveDevice(keyboard);
    }

    IEnumerator CheckPresentation()
    {
        bool endingsOnly = SessionState.GetBool("InspectFlightEndings", false);
        SessionState.SetBool("InspectFlightEndings", false);
        if (!endingsOnly)
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
        while (GameOverState.Active || (hub = FindAnyObjectByType<HangarHub>()) == null || CloudTransition.Busy || HangarHub.HoldingShip || HangarShutter.Active)
            yield return null;
        if (!endingsOnly)
        {
            Call("OpenDock", 0);
            yield return Capture("StampedHull");
            Call("CloseDock");
        }
        Call("Switch", ShipFrame.Lancer);
        hub.StartCoroutine((IEnumerator)typeof(HangarHub).GetMethod("Launch", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(hub, null));
        while (FindAnyObjectByType<WaveOrchestrer>() == null || HangarHub.Launching || CloudTransition.Busy)
            yield return null;
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
