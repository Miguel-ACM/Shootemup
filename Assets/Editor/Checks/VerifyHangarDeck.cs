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
    public static void FilterPass()
    {
        SessionState.SetBool("DirectoryOnly",true);
        Rooms();
    }
    public static void Rooms()
    {
        SessionState.SetBool("InspectPhysicalRooms", true);
        Run();
    }
    public static void Stations()
    {
        SessionState.SetBool("StationArtOnly", true);
        Rooms();
    }
    public static void Lane()
    {
        SessionState.SetBool("InspectDockLane", true);
        Run();
    }
    public static void Arrival()
    {
        SessionState.SetBool("InspectDockArrival", true);
        Run();
    }
    public static void Outfitter()
    {
        SessionState.SetBool("InspectOutfitter", true);
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
    Gamepad menuPad;
    string error;
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!project.Replace('\\', '/').EndsWith("/Temp/DecisionHoverReview", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Dock review needs its disposable checkout");
        // Only the simulated pilot should move or change prompts during these captures.
        foreach (var device in InputSystem.devices)
            if (device is Keyboard || device is Gamepad)
                InputSystem.DisableDevice(device, keepSendingEvents: true);
        var scratch = Directory.CreateDirectory(Path.Combine(project, "Temp", "PilotReview", Guid.NewGuid().ToString("N")));
        MetaProgress.ReviewSaveDirectory = scratch.FullName;
        if (SessionState.GetBool("InspectOutfitter", false) || SessionState.GetBool("InspectFlightEndings", false))
            File.WriteAllText(Path.Combine(scratch.FullName, "hangar.json"),
                "{\"version\":2,\"lastFrame\":1,\"coins\":9999,\"ship\":\"GR07\",\"unlocked\":[\"Popcorn\"],\"stock\":[\"SaltShaker\",\"LuckyPenny\",\"RollerBearings\"],\"loadout\":[]}");
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
        {
            error = message;
            File.WriteAllText(VerifyHangarDeck.Folder + "/Result.txt", "FAIL " + message);
        }
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
    IEnumerator CheckItemReveal(CanvasGroup group, string capture = null)
    {
        var frame = new WaitForEndOfFrame();
        float previous = group.alpha, until = Time.unscaledTime + 1;
        bool intermediate = false;
        Check(group.gameObject.activeInHierarchy && previous < .25f, "new description starts transparent");
        while (group.alpha < 1 && Time.unscaledTime < until)
        {
            yield return frame;
            if (group.alpha < previous)
                throw new Exception("description restarted during the same hover");
            previous = group.alpha;
            if (!intermediate && previous > .2f && previous < .9f)
            {
                intermediate = true;
                if (capture != null)
                    ScreenCapture.CaptureScreenshot(VerifyHangarDeck.Folder + "/" + capture + ".png");
            }
        }
        Check(intermediate && group.alpha == 1, "description passes through partial opacity and reaches full opacity");
    }
    IEnumerator ReviewPlatformMotion(MeshRenderer platform, MeshRenderer exhibit)
    {
        var separation = exhibit.bounds.center - platform.bounds.center;
        float low = platform.bounds.center.y, high = low;
        const int frames = 60;
        float start = Time.time, period = 2 * Mathf.PI / 1.1f;
        var folder = Directory.CreateDirectory(VerifyHangarDeck.Folder + "/Hover");
        for (int frame = 0; frame < frames; frame++)
        {
            while (Time.time < start + period * frame / frames)
                yield return null;
            yield return new WaitForEndOfFrame();
            low = Mathf.Min(low, platform.bounds.center.y);
            high = Mathf.Max(high, platform.bounds.center.y);
            if ((exhibit.bounds.center - platform.bounds.center - separation).sqrMagnitude > .0001f)
                throw new Exception("exhibit shifted on its floating platform");
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(folder.FullName, frame.ToString("D2") + ".png"), image.EncodeToPNG());
            Destroy(image);
        }
        Check(high - low > .22f, "item and platform floated together across a full hover cycle");
    }
    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(1);
        if (SessionState.GetBool("InspectDockArrival", false))
        {
            yield return ReviewArrival();
            yield break;
        }
        if (SessionState.GetBool("InspectPhysicalRooms", false) || SessionState.GetBool("InspectPressureDoor", false))
            StartCoroutine(MeasurePopulation());
        if (SessionState.GetBool("InspectPhysicalRooms", false) && !SessionState.GetBool("StationArtOnly", false))
        {
            var entry = FindAnyObjectByType<SettingsEntry>();
            var menu = entry.GetComponentInParent<Menu>();
            MetaProgress.FlightSetting = RunDifficulty.Hard;
            menu.TryAccept(entry);
            yield return null;
            var erase = FindObjectsByType<SettingsErase>().Single(row => row.choice == 0);
            menu.TryAccept(erase);
            yield return null;
            Check(menu.CurrentElement.name == "KEEP SAVE", "erasure defaults to keeping the save");
            menu.TryAccept(menu.CurrentElement);
            Check(MetaProgress.FlightSetting == RunDifficulty.Hard, "cancelling keeps progress");
            menu.TryAccept(erase);
            yield return null;
            menu.TryAccept(FindObjectsByType<SettingsErase>().Single(row => row.choice == 2));
            Check(MetaProgress.FlightSetting == RunDifficulty.Normal && MetaProgress.Stock.Count == 3, "confirmed erase resets the isolated pilot");
        }
        if (SessionState.GetBool("InspectFlightEndings", false))
        {
            var entry = FindAnyObjectByType<SettingsEntry>();
            var menu = entry.GetComponentInParent<Menu>();
            menu.setCurrentElement(entry);
            yield return TapMenuButton(GamepadButton.South);
            var erase = FindObjectsByType<SettingsErase>().Single(row => row.choice == 0);
            menu.setCurrentElement(erase);
            yield return TapMenuButton(GamepadButton.South);
            menu.setCurrentElement(FindObjectsByType<SettingsErase>().Single(row => row.choice == 2));
            yield return TapMenuButton(GamepadButton.East);
            Check(menu.CurrentElement == erase, "Circle cancels erase even when the destructive row is selected");
            yield return TapMenuButton(GamepadButton.East);
            Check(menu.CurrentElement == entry && menu.IsOpen, "Circle leaves Settings one level at a time");
            menu.setCurrentElement(FindAnyObjectByType<NewGame>());
            yield return TapMenuButton(GamepadButton.South);
        }
        else FindAnyObjectByType<NewGame>().OnAccept();
        if(SessionState.GetBool("InspectPhysicalRooms",false) || SessionState.GetBool("InspectPressureDoor",false))
        {
            while (!HangarHub.Approaching) yield return null;
            yield return Capture("DockApproach");
            var camera = UnityEngine.Camera.main;
            float enteringY = camera.transform.position.y;
            yield return new WaitForSecondsRealtime(.6f);
            Check(camera.transform.position.y > enteringY + .5f, "live camera travels from the title into the dock");
        }
        while ((hub = FindAnyObjectByType<HangarHub>()) == null || CloudTransition.Busy || HangarHub.HoldingShip)
            yield return null;
        yield return new WaitForSecondsRealtime(2);
        if (SessionState.GetBool("InspectOutfitter", false))
        {
            SessionState.SetBool("InspectOutfitter", false);
            yield return OutfitterAndBerths();
            Check(error == null, error ?? "no gameplay errors in the outfitter or fleet");
            File.WriteAllText(VerifyHangarDeck.Folder + "/Result.txt", "PASS");
            yield break;
        }
        if(SessionState.GetBool("InspectPressureDoor",false))
        {
            SessionState.SetBool("InspectPressureDoor",false);
            yield return Capture("PhysicalLane");
            var pilot = FindAnyObjectByType<Shipcontroller>().transform;
            pilot.position = new Vector3(-12.75f, 1.6f, pilot.position.z);
            yield return new WaitForSeconds(.8f);
            yield return Capture("FleetTube");
            pilot.position = new Vector3(11.4f, 2.85f, pilot.position.z);
            yield return new WaitForSeconds(.8f);
            yield return Capture("FilterPrompt");
            var keys = InputSystem.AddDevice<Keyboard>();
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.QueueStateEvent(keys, new KeyboardState(Key.UpArrow));
            InputSystem.Update();
            float crossBy = Time.realtimeSinceStartup + 3;
            while (pilot.position.y < 10.15f && Time.realtimeSinceStartup < crossBy)
                yield return null;
            InputSystem.RemoveDevice(keys);
            Check(pilot.position.y >= 10.15f && !HangarHub.HoldingShip, "pilot can fly over the gallery screen");
            yield return Capture("ScreenFlyover");
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
        yield return DismissResult(GamepadButton.South);
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
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var keys = InputSystem.AddDevice<Keyboard>();
        void Press(params Key[] state)
        {
            InputSystem.QueueStateEvent(keys, new KeyboardState(state));
            InputSystem.Update();
        }
        var pilot = FindAnyObjectByType<Shipcontroller>();
        var ship = pilot.transform;
        var camera = UnityEngine.Camera.main;
        var view = camera.transform;
        float cameraOrigin = 0, spawn = ship.position.y;
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
        while (HangarHub.Active || !pilot.enabled) yield return null;
        Check(pilot.GetComponent<LoadoutHUD>().Panels.All(panel => panel != null) &&
              FindAnyObjectByType<Warning>().ScreenRect != null &&
              GameObject.Find("Score, multiplier and wallet") != null,
              "flight instruments and warning display are prepared before control returns");
        var hull = ship.GetComponent<PSXShipVisual>().Model.GetComponentsInChildren<MeshRenderer>().First(r => r.enabled);
        var frameEnd = new WaitForEndOfFrame();
        do
        {
            yield return frameEnd; // Include the HUD's LateUpdate clamp.
            var low = camera.WorldToViewportPoint(hull.bounds.min);
            var high = camera.WorldToViewportPoint(hull.bounds.max);
            float halfWidth = .5f * (405f / 720f) / camera.aspect;
            if (low.y < 0 || high.y > 1 || low.x < .5f - halfWidth || high.x > .5f + halfWidth)
                throw new Exception($"Plains launch put the pilot outside the portrait: ship {ship.position}, camera {view.position}");
        }
        while (HangarHub.Launching || CloudTransition.Busy);
        Check(Mathf.Abs(view.position.y - cameraOrigin) < .05f && Mathf.Abs(ship.position.y - spawn) < .1f, "launch restores combat camera and ship position");
        yield return Capture("PlainsPlayerSpawn");
        Press(Key.UpArrow);
        yield return new WaitForSeconds(2);
        Check(ship.position.y >= Camera.UpLimit - .05f, "pilot can cross Plains to its upper combat limit");
        Press(Key.DownArrow);
        yield return new WaitForSeconds(2.2f);
        Press();
        yield return frameEnd;
        float cockpitBottom = camera.WorldToViewportPoint(ship.GetComponent<Collider2D>().bounds.min).y;
        Check(Mathf.Abs(cockpitBottom) < .005f, "the cockpit can reach the bottom edge of the actual playfield");
        InputSystem.RemoveDevice(keys);
    }

    static string Frames(System.Collections.Generic.List<float> samples)
    {
        if (samples.Count == 0)
            return "no samples";
        samples.Sort();
        return samples.Count + " frames, p95 " + samples[Mathf.CeilToInt(samples.Count * .95f) - 1].ToString("F1") + " ms, max " + samples[samples.Count - 1].ToString("F1") + " ms";
    }
    IEnumerator ReviewArrival()
    {
        while (!LevelEnvironment.TitleReady)
            yield return null;
        var landmark = GameObject.Find("Title tunnel backdrop").transform.GetChild(0);
        GameObject aircraft = null;
        var trace = new System.Text.StringBuilder("seconds,frame_ms,scene,tunnel_y,ship_y\n");
        var frames = Directory.CreateDirectory(VerifyHangarDeck.Folder + "/Arrival");
        var end = new WaitForEndOfFrame();
        float start = Time.unscaledTime, nextCapture = 0, highestShipY = float.NegativeInfinity;
        float previousY = float.NaN, reverse = 0;
        bool tunnelStopped = false;
        bool boarded = false;
        FindAnyObjectByType<NewGame>().OnAccept();
        do
        {
            yield return end;
            var camera = UnityEngine.Camera.main;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (scene == "L1" && aircraft == null)
                aircraft = GameObject.FindGameObjectWithTag("Player");
            float elapsed = Time.unscaledTime - start;
            float shipY = aircraft == null ? float.NaN : aircraft.transform.position.y - camera.transform.position.y;
            float tileY = landmark.position.y - camera.transform.position.y;
            trace.AppendLine(FormattableString.Invariant($"{elapsed:F4},{Time.unscaledDeltaTime * 1000:F2},{scene},{tileY:F4},{shipY:F4}"));
            if (elapsed >= nextCapture)
            {
                ScreenCapture.CaptureScreenshot(Path.Combine(frames.FullName, Time.frameCount.ToString("D6") + ".png"));
                nextCapture = elapsed + .24f;
            }
            if (aircraft != null)
            {
                boarded = true;
                if (shipY < -camera.orthographicSize && tileY == previousY)
                    tunnelStopped = true;
                reverse = Mathf.Max(reverse, highestShipY - shipY);
                highestShipY = Mathf.Max(highestShipY, shipY);
            }
            previousY = tileY;
        } while (!boarded || HangarHub.HoldingShip || CloudTransition.Busy);
        File.WriteAllText(Path.Combine(frames.FullName, "trace.csv"), trace.ToString());
        Check(reverse < .04f, $"ship advances continuously into its berth (reversal {reverse:F3})");
        Check(!tunnelStopped, "live tunnel keeps moving across scene activation");
        if (error == null) File.WriteAllText(VerifyHangarDeck.Folder + "/Result.txt", "PASS");
    }

    IEnumerator MeasurePopulation()
    {
        HangarHub deck;
        while ((deck = FindAnyObjectByType<HangarHub>()) == null)
            yield return null;
        var ready = typeof(HangarHub).GetField("galleryReady", BindingFlags.Instance | BindingFlags.NonPublic);
        var timings = new System.Collections.Generic.List<float>();
        while (!(bool)ready.GetValue(deck))
        {
            yield return null;
            timings.Add(Time.unscaledDeltaTime * 1000);
        }
        Debug.Log("Gallery population " + Frames(timings));
    }
    IEnumerator OutfitterAndBerths()
    {
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var keys = InputSystem.AddDevice<Keyboard>();
        void Press(params Key[] state)
        {
            InputSystem.QueueStateEvent(keys, new KeyboardState(state));
            InputSystem.Update();
        }
        var ship = FindAnyObjectByType<Shipcontroller>().transform;
        void Place(float x, float y) => ship.position = new Vector3(x, y, ship.position.z);
        T Field<T>(string name) => (T)typeof(HangarHub).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hub);
        var offers = hub.GetComponentsInChildren<AItem>(true).OrderBy(o => o.transform.position.y).ToArray();
        Check(offers.Length == 3 && offers.All(o => !o.gameObject.activeInHierarchy && o.transform.position.x < -6),
            "loaded platforms wait in the port bay before the shop comes into view");
        Place(0, 14.25f);
        var discs = offers.Select(o => o.transform.parent.Find("HangarShopPlinth")).ToArray();
        var inFlight = new bool[3];
        float finish = Time.time + 2.5f;
        while (Time.time < finish)
        {
            yield return new WaitForEndOfFrame();
            for (int i = 0; i < offers.Length; i++)
            {
                if (!offers[i].gameObject.activeInHierarchy) continue;
                var at = offers[i].transform.position;
                if ((discs[i].position - at - new Vector3(0, -.27f, 1.3f)).sqrMagnitude > .0001f)
                    throw new Exception("item slipped off its platform during delivery");
                if (at.x > -7.5f && at.x < -4) inFlight[i] = true;
            }
        }
        Check(inFlight.All(seen => seen), "all three platforms carried their items in from the left");
        Check(offers[0].transform.position.y > 6 && offers[2].transform.position.y < 20 &&
            offers[0].transform.position.x < -2 && offers[1].transform.position.x > 2 && offers[2].transform.position.x < -2 &&
            offers[1].GetData()[0] == "Lucky Penny", "stock alternates left, right, left between the paired doors");
        yield return Capture("OutfitterSpacing");
        var rows = hub.GetComponentsInChildren<HorizontalLayoutGroup>(true).Where(row => row.transform.parent.name == "Stock placard").ToArray();
        var classes = hub.GetComponentsInChildren<RectTransform>(true).Where(r => r.name == "Equipment class").ToArray();
        var wallet = Field<RectTransform>("walletPanel");
        var details = Field<RectTransform>("stockScreen");
        Check(rows.Length == 3 && classes.Length == 3 && rows.All(r => !r.isActiveAndEnabled) &&
            classes.All(r => !r.gameObject.activeInHierarchy) && wallet.gameObject.activeSelf && !details.gameObject.activeSelf,
            "the whole outfitter lane shows the balance while unattended platforms show only merchandise");
        Place(offers[0].transform.position.x + 1.6f, offers[0].transform.position.y);
        yield return null;
        Check(wallet.gameObject.activeSelf && rows.All(r => !r.isActiveAndEnabled) && !details.gameObject.activeSelf,
            "approaching lights the bank without revealing an offer");
        foreach (var offer in offers)
        {
            Place(offer.transform.position.x, offer.transform.position.y);
            yield return null;
            yield return CheckItemReveal(details.GetComponent<CanvasGroup>(), offer == offers[1] ? "OutfitterMidFade" : null);
            var row = rows.Single(r => r.isActiveAndEnabled);
            Check(classes.Count(r => r.gameObject.activeInHierarchy) == 1 && details.gameObject.activeSelf &&
                row.transform.parent.GetComponentsInChildren<TMPro.TMP_Text>().Any(t => t.text == offer.GetData()[0]) &&
                Field<TMPro.TMP_Text>("stockInfo").text == offer.GetData()[2], "only the hovered offer reveals its identity and details");
            var icon = row.GetComponentInChildren<Image>().rectTransform;
            var number = row.GetComponentInChildren<TMPro.TMP_Text>().rectTransform;
            var a = new Vector3[4];
            var b = new Vector3[4];
            icon.GetWorldCorners(a);
            number.GetWorldCorners(b);
            Check(icon.GetComponent<Image>().sprite.name == "DockCoin" && a[2].x - a[0].x >= 12 &&
                b[0].x - a[2].x >= 4 && Mathf.Abs(icon.position.y - number.position.y) < 1,
                "stamped coin and amount have separate, aligned bounds at gameplay size");
            Check(wallet.gameObject.activeSelf && wallet.anchoredPosition.x < 0 &&
                Field<TMPro.TMP_Text>("walletText").text == "9999", "balance lights on the left for this offer");
        }
        yield return Capture("OutfitterPortPrice");
        Place(offers[1].transform.position.x, offers[1].transform.position.y);
        Press(Key.LeftShift);
        yield return null;
        Press();
        yield return Capture("OutfitterPrice");
        var buy = Field<RawImage>("buyIcon");
        var pin = Field<RawImage>("pinIcon");
        bool Glyph(RawImage icon, string art, string key) => icon.texture.name == art && icon.GetComponentInChildren<TMPro.TMP_Text>().text == key;
        Check(Glyph(buy, "KeyPrompt", "Z") && Glyph(pin, "KeyPrompt", "X"), "buy and pin show their keyboard keycaps");
        var pad = InputSystem.AddDevice<Gamepad>();
        InputSystem.QueueStateEvent(pad, new GamepadState { rightStick = new Vector2(.16f, 0) });
        InputSystem.Update();
        yield return Capture("OutfitterBumpers");
        Check(Glyph(buy, "FacePrompt", "X") && Glyph(pin, "PadPrompt", "LB"), "hover shows Cross to buy and the left bumper to pin");
        InputSystem.RemoveDevice(pad);
        Check(Field<RectTransform>("walletPanel").gameObject.activeSelf && Field<TMPro.TMP_Text>("walletText").text == "9999", "nearby wallet reads the full bank");
        Press(Key.X);
        yield return null;
        Press();
        Check(MetaProgress.Pin == "LuckyPenny", "secondary pins the approached offer");
        int cost = MetaProgress.Price(offers[1].rarity);
        Press(Key.Z);
        yield return null;
        Press();
        Check(!MetaProgress.Stock.Contains("LuckyPenny") && MetaProgress.Coins == 9999 - cost, "buying debits the displayed price");
        Check(rows.All(r => !r.isActiveAndEnabled) && !details.gameObject.activeSelf, "the purchased offer closes immediately");
        var lights = hub.GetComponentsInChildren<MeshFilter>().Where(f => f.name == "Portal illumination").ToArray();
        Check(lights.Length == 5 && lights.All(f => {
            var mesh = f.sharedMesh;
            var vertices = mesh.vertices;
            var colors = mesh.colors;
            return Enumerable.Range(0, vertices.Length).All(i => Mathf.Abs(vertices[i].x) < mesh.bounds.extents.x - .01f || colors[i].a == 0) &&
                colors.Any(c => c.a > .8f);
        }), "every threshold has a luminous core and transparent ends on both sides");
        Place(-7.7f, 6);
        yield return new WaitForSeconds(.8f);
        yield return Capture("FleetThreshold");
        Place(7.7f, 6);
        yield return new WaitForSeconds(.8f);
        yield return Capture("ArmoryThreshold");
        Place(24.8f, 12.9f);
        yield return new WaitForSeconds(.8f);
        var card = Field<RectTransform>("cardPanel").GetComponent<CanvasGroup>();
        Place(24.8f, 14.8f);
        yield return null;
        yield return CheckItemReveal(card, "GalleryMidFade");
        Place(28.95f, 14.8f);
        yield return null;
        yield return CheckItemReveal(card);
        var dialog = FindAnyObjectByType<Dialog>();
        var runCard = dialog.Panel.GetComponentInParent<CanvasGroup>();
        foreach (var item in new[] { offers[0], offers[2] })
        {
            var data = item.GetData();
            dialog.ShowItemDesc(data[0], data[1], data[2], item.rarity);
            yield return CheckItemReveal(runCard);
        }
        dialog.Hide();
        Place(-23.6f, 6);
        yield return new WaitForSeconds(.8f);
        yield return Capture("FleetBerths");
        var hulls = Field<System.Collections.Generic.List<Transform>>("parkedShips");
        for (int i = 0; i < hulls.Count; i++)
        {
            bool locked = !MetaProgress.CanFly((ShipFrame)i);
            Check(hulls[i].GetComponentsInChildren<MeshRenderer>(true).All(r => r.sharedMaterials.All(m =>
                (m.shader.name == "Shootemup/Gallery silhouette") == locked)), "fleet paint follows the hull's unlock");
        }
        var ember = hulls[(int)ShipFrame.Ember].position;
        Place(ember.x, ember.y);
        yield return new WaitForSeconds(.5f);
        Press(Key.Z);
        yield return null;
        Press();
        Check(MetaProgress.Ship == ShipFrame.GR07 && Field<TMPro.TMP_Text>("berthNotice").text.Contains("Lancer"),
            "locked berth explains its release condition and cannot equip");
        Place(-12.75f, 1.6f);
        yield return Capture("FleetLockedRecord");
        var portrait = GameObject.Find("FLEET mounted screen").GetComponentsInChildren<RawImage>()
            .Single(im => im.texture.name == "PortraitEmber");
        Check(portrait.color == Color.black, "locked fleet portrait is a silhouette");
        var lancer = hulls[(int)ShipFrame.Lancer].position;
        Place(lancer.x, lancer.y);
        yield return new WaitForSeconds(.4f);
        Press(Key.Z);
        yield return null;
        Press();
        Check(MetaProgress.Ship == ShipFrame.Lancer && !hulls[(int)ShipFrame.Lancer].gameObject.activeSelf &&
            hulls[(int)ShipFrame.GR07].gameObject.activeSelf, "commissioned hull swaps normally and leaves its berth empty");
        InputSystem.RemoveDevice(keys);
    }

    IEnumerator PhysicalRooms()
    {
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        void Press(params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            InputSystem.Update();
        }
        var ship = FindAnyObjectByType<Shipcontroller>().transform;
        void Place(float x, float y)
        {
            ship.position = new Vector3(x, y, ship.position.z);
        }
        T Field<T>(string name) => (T)typeof(HangarHub).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hub);
        while (!Field<bool>("galleryReady"))
            yield return null;
        Check(Field<int>("loadedItems") == ItemCatalog.Exhibits.Count, "every catalog entry installed");
        string[] roomSigns = { "FLEET", "ARMORY", "SIMULATOR", "RECORDER", "FLIGHT CONTROL" };
        void CheckInteriors(int entered)
        {
            bool correct = true;
            for (int i = 0; i < roomSigns.Length; i++)
            {
                bool visible = i == entered;
                var world = hub.transform.Find(roomSigns[i] + " interior");
                var displays = GameObject.Find(roomSigns[i] + " displays").GetComponent<CanvasGroup>();
                correct &= world.gameObject.activeInHierarchy == visible && displays.alpha == (visible ? 1 : 0);
            }
            Check(correct, entered < 0 ? "main lane hides every side room" : "only the entered interior and its glass are visible");
        }
        bool directoryOnly = SessionState.GetBool("DirectoryOnly", false);
        SessionState.SetBool("DirectoryOnly", false);
        if (directoryOnly)
        {
            Place(0, 14.25f);
            yield return new WaitForSeconds(2);
            yield return Capture("HexOutfitter");
        }
        if (!directoryOnly)
        {
            Place(2.55f, 14.25f);
            yield return new WaitForSeconds(2);
            yield return Capture("CRTOutfitter");
            Place(0, 6);
            yield return new WaitForSeconds(.8f);
            yield return Capture("LitPassage");
            CheckInteriors(-1);
            var sign = GameObject.Find("ARMORY lift sign").transform;
            var portJet = sign.Find("Port lift exhaust");
            var starboardJet = sign.Find("Starboard lift exhaust");
            Check(portJet.localPosition.x < 0 && starboardJet.localPosition.x > 0 &&
                portJet.GetComponent<MeshRenderer>().sharedMaterial == ship.GetComponent<PSXShipEffects>().exhaustMaterial,
                "direction sign has separate port and starboard lift exhausts");
            float beforeLift = sign.position.y, beforeThrust = portJet.localScale.y;
            yield return new WaitForSeconds(.2f);
            Check(Mathf.Abs(beforeLift - sign.position.y) > .0001f && Mathf.Abs(beforeThrust - portJet.localScale.y) > .0001f,
                "sign hovers and the attached engines pulse");
            var camera = UnityEngine.Camera.main;
            for (int pass = 0; pass < 4; pass++)
            {
                bool boost = pass % 2 != 0;
                int side = pass < 2 ? 1 : -1;
                Place(0, 6);
                yield return null;
                float expectedSpeed = GameRules.playerMoveSpeed * (boost ? 2 : 1);
                for (int direction = 1; direction >= -1; direction -= 2)
                {
                    Key steer = side * direction > 0 ? Key.RightArrow : Key.LeftArrow;
                    if (boost) Press(steer, Key.LeftShift);
                    else Press(steer);
                    float started = Time.time, peakPan = 0, peakAtX = 0, furthestOffset = 0;
                    float laneDistance = 0, laneTime = 0, worstDoorError = 0;
                    float previousX = camera.transform.position.x;
                    while ((direction > 0 ? side * ship.position.x < 12.2f : side * ship.position.x > .2f) && Time.time - started < 6)
                    {
                        float from = side * ship.position.x;
                        yield return null;
                        float distance = (side * ship.position.x - from) * direction;
                        if (from > .35f && from < 3)
                        {
                            laneDistance += distance;
                            laneTime += GameClock.PlayerDelta;
                        }
                        if (from >= 3.35f && from <= 12.2f && GameClock.PlayerDelta > 0)
                            worstDoorError = Mathf.Max(worstDoorError, Mathf.Abs(distance / GameClock.PlayerDelta - expectedSpeed));
                        float x = camera.transform.position.x;
                        float panSpeed = Mathf.Abs(x - previousX) / Mathf.Max(.001f, Time.deltaTime);
                        if (panSpeed > peakPan)
                        {
                            peakPan = panSpeed;
                            peakAtX = ship.position.x;
                        }
                        furthestOffset = Mathf.Max(furthestOffset, Mathf.Abs(ship.position.x - x));
                        previousX = x;
                    }
                    Press();
                    Check(Time.time - started < 6 && laneTime > 0 && Mathf.Abs(laneDistance / laneTime - expectedSpeed) < .02f &&
                        worstDoorError < .02f, $"ship holds {expectedSpeed:F1} units/s in lane and doorway, direction {direction}");
                    Check(peakPan < 29 &&
                        furthestOffset < camera.orthographicSize * 405f / 720f,
                        $"passage at {expectedSpeed:F1} units/s, direction {direction}: peak pan {peakPan:F1} at x={peakAtX:F2}, offset {furthestOffset:F2}");
                    if (direction > 0)
                    {
                        float stopped = ship.position.x, framing = camera.transform.position.x;
                        yield return new WaitForSeconds(.4f);
                        float drift = Mathf.Abs(ship.position.x - stopped), pan = side * (camera.transform.position.x - framing);
                        Check(drift < .001f && pan > .2f, $"after release the ship moved {drift:F3} and the camera settled {pan:F2} units");
                    }
                }
            }
            Place(-12.75f, 1.6f);
            yield return new WaitForSeconds(.8f);
            yield return Capture("FleetTube");
            CheckInteriors(0);
            Press(Key.Z);
            yield return null;
            Press();
            Check(HangarHub.HoldingShip && GameObject.Find("FLEET mounted screen").GetComponentsInChildren<TMPro.TMP_Text>()
                .Any(t => t.text.Contains("BOSS RECORD")), "throttle opens the full fleet service record");
            yield return new WaitForSeconds(.65f);
            yield return Capture("FleetService");
            Press(Key.X);
            yield return null;
            Press();
            // Fly the length of the new apron through the normal controller.
            Press(Key.LeftArrow, Key.LeftShift);
            float untilBerths = Time.realtimeSinceStartup + 4;
            while (ship.position.x > -23.55f && Time.realtimeSinceStartup < untilBerths)
                yield return null;
            Press();
            Check(ship.position.x <= -23.55f && Field<int>("room") == 0, "fleet apron reachable from control desk");
            Place(-23.65f, 6);
            yield return new WaitForSeconds(.8f);
            yield return Capture("FleetBerths");
            var screen = GameObject.Find("FLEET mounted screen");
            Check(screen != null && !Field<System.Collections.Generic.List<Transform>>("parkedShips")[(int)MetaProgress.Ship].gameObject.activeSelf, "flown hull has no duplicate on the pad");
            Place(0, 6);
            yield return new WaitForSeconds(.4f);
            Check(screen.activeInHierarchy, "the fleet screen remains mounted after exit");
            CheckInteriors(-1);
            foreach (var berth in new[] { new Vector2(-11, 20), new Vector2(11, 20), new Vector2(11, 34) })
            {
                Place(Mathf.Sign(berth.x) * 12.75f, berth.y - 4.4f);
                yield return new WaitForSeconds(.7f);
                Check(Field<int>("room") == (berth.x < 0 ? 2 : berth.y == 20 ? 3 : 4), "reached the marked control room");
                CheckInteriors(Field<int>("room"));
                yield return Capture(berth.x < 0 ? "SimulatorRoom" : berth.y == 20 ? "RecorderRoom" : "FlightRoom");
                Press(Key.Z);
                yield return null;
                Press();
                Check(HangarHub.HoldingShip, "room instrument accepts control at its physical station");
                Press(Key.X);
                yield return null;
                Press();
                Check(!HangarHub.HoldingShip, "pilot can leave the station");
            }
        }
        if (SessionState.GetBool("StationArtOnly", false))
        {
            SessionState.SetBool("StationArtOnly", false);
            Place(11.4f, 2.85f);
            yield return new WaitForSeconds(.7f);
            yield return Capture("FilterPrompt");
            Press(Key.Z);
            yield return null;
            Press();
            Call("Navigate", true, 1);
            yield return new WaitForSeconds(.7f);
            yield return Capture("FilterScreen");
            InputSystem.RemoveDevice(keyboard);
            yield break;
        }
        yield return null;
        yield return null; // Keep screenshot readback out of the flight sample.
        var entryFrames = new System.Collections.Generic.List<float>();
        Place(8, 6);
        for (int frame = 0; frame < 32; frame++)
        {
            yield return null;
            entryFrames.Add(Time.unscaledDeltaTime * 1000);
        }
        Debug.Log("Gallery entry " + Frames(entryFrames));
        var exhibits = Field<GalleryShelf.Exhibit[]>("exhibits");
        var models = Field<MeshRenderer[]>("pieces");
        var platforms = Field<MeshRenderer[]>("platforms");
        var shopMesh = hub.transform.Find("Loaded outfitter platform/HangarShopPlinth").GetComponent<MeshFilter>().sharedMesh;
        var outerCorners = shopMesh.vertices.Select(v => (Vector2)v).Where(p => Mathf.Abs(p.sqrMagnitude - .25f) < .0001f).Distinct().Count();
        Check(outerCorners == 6 && platforms.All(p => p.GetComponent<MeshFilter>().sharedMesh == shopMesh),
            "gallery and outfitter share the six-sided floating plinth");
        CheckInteriors(1);
        void CheckCaseLighting()
        {
            var offsets = Field<int[]>("caseVertices");
            var colors = Field<Mesh>("floorLights").colors;
            var category = Field<int>("galleryCategory");
            var access = Field<int>("galleryAvailability");
            bool correct = true;
            for (int i = 0; i < exhibits.Length; i++)
            {
                bool released = !ItemCatalog.Get(exhibits[i].key).InRewardPool || MetaProgress.IsUnlocked(exhibits[i].key);
                bool match = (category == 0 || category - 1 == (int)exhibits[i].category) && (access == 0 || released == (access == 1));
                bool lampLit = colors[offsets[i + 1] - 1].b > .8f;
                correct &= lampLit == match && platforms[i].HasPropertyBlock() == !match && models[i].HasPropertyBlock() == (released && !match);
            }
            Check(correct, "all 99 case lamps and exhibit finishes follow both filters");
        }
        CheckCaseLighting();
        int known = System.Array.FindIndex(exhibits, e => MetaProgress.IsUnlocked(e.key));
        int locked = System.Array.FindIndex(exhibits, e => ItemCatalog.Get(e.key).InRewardPool && !MetaProgress.IsUnlocked(e.key));
        void At(int i)
        {
            Place(24.8f + i % GalleryShelf.Columns * 4.15f, 14.8f + i / GalleryShelf.Columns * 3.8f);
        }
        At(known);
        yield return new WaitForSeconds(.8f);
        yield return Capture("GalleryReleased");
        var title = Field<TMPro.TextMeshProUGUI>("cardTitle");
        var body = Field<TMPro.TextMeshProUGUI>("cardText");
        var source = Field<TMPro.TextMeshProUGUI>("cardSource");
        Check(title.text == exhibits[known].title && body.text == exhibits[known].description && title.fontSize > body.fontSize * 1.4f,
            "the released exhibit has a prominent name above its description");
        At(locked);
        yield return new WaitForSeconds(.8f);
        yield return Capture("GalleryLocked");
        Check(title.text == "???" && body.text == "???" && source.text.Length > 0 &&
            models[locked].sharedMaterial.shader.name == "Shootemup/Gallery silhouette", "locked hull shape, redacted labels and unlock source");
        Place(11.4f, 2.85f);
        yield return new WaitForSeconds(.8f);
        yield return Capture("FilterPrompt");
        var pad = InputSystem.AddDevice<Gamepad>();
        InputSystem.QueueStateEvent(pad, new GamepadState { rightStick = new Vector2(.16f, 0) });
        InputSystem.Update();
        yield return null;
        yield return Capture("GamepadPrompt");
        var roomIcon = Field<RawImage>("inputPicture");
        Check(roomIcon.GetComponentInChildren<TMPro.TMP_Text>().text == "X" && roomIcon.texture.name == "FacePrompt", "room console shows the confirm face button");
        InputSystem.RemoveDevice(pad);
        Press(Key.Z);
        yield return null;
        Press();
        Check(HangarHub.HoldingShip, "filter console accepts bound key");
        var timings = new System.Collections.Generic.List<float>();
        for (int i = 0; i < 24; i++)
        {
            if (i % 4 == 0)
                Press(Key.RightArrow);
            if (i % 4 == 2)
                Press();
            yield return null;
            timings.Add(Time.unscaledDeltaTime * 1000);
        }
        Press();
        Debug.Log("Gallery filter " + Frames(timings));
        Press(Key.RightArrow);
        yield return null;
        Press();
        yield return null;
        Check(Field<Mesh>("guideMesh").vertexCount > 0, "matching exhibits linked to bottom console");
        Check(Field<Mesh>("guideMesh").colors.All(c => c.b > c.g && c.g > c.r), "guide paths carry the directory's blue");
        CheckCaseLighting();
        Call("Navigate", false, 1);
        CheckCaseLighting();
        Call("Navigate", false, -1);
        yield return Capture("FilterScreen");
        Press(Key.X);
        yield return null;
        Press();
        Place(33.1f, 22.4f);
        yield return new WaitForSeconds(.8f);
        yield return Capture("GalleryGuides");
        Place(0, 6);
        yield return null;
        CheckInteriors(-1);
        Place(11.4f, 2.85f);
        yield return null;
        CheckInteriors(1);
        CheckCaseLighting();
        while (Field<int>("galleryCategory") != 0)
            Call("Navigate", true, -1);
        Check(Field<Mesh>("guideMesh").vertexCount == 0, "clearing the filters removes the routes");
        CheckCaseLighting();
        if (directoryOnly)
        {
            var anchor = platforms[known].transform.parent.position;
            Place(anchor.x + 1.4f, anchor.y - 1.3f);
            yield return new WaitForSeconds(.8f);
            yield return ReviewPlatformMotion(platforms[known], models[known]);
            InputSystem.RemoveDevice(keyboard);
            yield break;
        }
        int secondary = Array.FindIndex(exhibits, e => e.category == AItem.ItemType.passiveWeapon);
        MetaProgress.UnlockAll();
        Call("RefreshGalleryAccess");
        At(secondary);
        yield return new WaitForSeconds(.6f);
        Check(source.text == "SECONDARY", "gallery prints SECONDARY on the item card");
        yield return Capture("GallerySecondary");
        int longest = Array.IndexOf(exhibits, exhibits.OrderByDescending(e => e.description.Length).First());
        At(longest);
        yield return new WaitForSeconds(.8f);
        yield return Capture("GalleryLongDescription");
        float Gap(RectTransform upper, RectTransform lower) =>
            upper.anchoredPosition.y - upper.rect.height * upper.pivot.y -
            (lower.anchoredPosition.y + lower.rect.height * (1 - lower.pivot.y));
        Check(body.text == exhibits[longest].description && !body.isTextOverflowing &&
            Gap(title.rectTransform, body.rectTransform) >= 17 && Gap(body.rectTransform, source.rectTransform) >= 17,
            "the longest description wraps with space above and below");
        Place(0, Camera.UpLimit + HangarHub.DeckExtension);
        yield return null;
        Check(Field<bool>("launching"), "entering the departure light launches without a button");
        float until = Time.realtimeSinceStartup + 35;
        while ((HangarHub.Active || CloudTransition.Busy || HangarHub.Launching) && Time.realtimeSinceStartup < until)
            yield return null;
        Press();
        while (!FindObjectsByType<AItem>().Any(a => a.Settled) && Time.realtimeSinceStartup < until)
            yield return null;
        var offers = FindObjectsByType<AItem>().Where(a => a.Settled && !a.Collected).ToArray();
        Check(offers.Length >= 1, "normal run starts with actual reward offers");
        foreach (var item in offers)
        {
            var visual = item.GetComponent<PSXObjectVisual>();
            Check(GalleryShelf.TryGet(item.GetType().Name, out var actual) && visual != null && visual.model.GetComponent<MeshFilter>().sharedMesh == actual.mesh,
                "run pickup displays its authored " + item.GetType().Name + " mesh");
        }
        yield return Capture("RunPickupModels");
        InputSystem.RemoveDevice(keyboard);
    }

    IEnumerator TapMenuButton(GamepadButton button)
    {
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        if (menuPad == null) menuPad = InputSystem.AddDevice<Gamepad>();
        InputSystem.QueueStateEvent(menuPad, new GamepadState().WithButton(button));
        yield return null;
        InputSystem.QueueStateEvent(menuPad, new GamepadState());
        yield return null;
    }

    IEnumerator DismissResult(GamepadButton button)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(13, 6 + MetaProgress.LastFlight.awards.Count * 1.4f));
        Check(GameOverState.Active && !CloudTransition.Busy, "flight tally remains open past its former timeout");
        yield return TapMenuButton(button);
        Check(CloudTransition.Busy, "a fresh face-button press returns to the hangar");
        while (GameOverState.Active || CloudTransition.Busy || !HangarHub.Active || HangarHub.HoldingShip)
            yield return null;
        Check(FindAnyObjectByType<HangarHub>() != null, "the return lands in the playable hangar");
    }

    void CheckResultLettering()
    {
        var font = FindAnyObjectByType<Dialog>().font;
        var labels = FindAnyObjectByType<GameOverState>().GetComponentsInChildren<TMPro.TextMeshProUGUI>();
        Check(labels.Length > 0 && labels.All(label => label.font == font && label.fontStyle == TMPro.FontStyles.Normal
            && !label.isTextOverflowing), "every flight result label fits in the regular game font");
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
        var craft = FindAnyObjectByType<Shipcontroller>().transform;
        if (endingsOnly)
        {
            craft.position = new Vector3(2.55f, 14.25f, craft.position.z);
            yield return new WaitForSeconds(2);
            int balance = MetaProgress.Coins;
            yield return TapMenuButton(GamepadButton.East);
            Check(MetaProgress.Coins == balance && MetaProgress.Pin == null, "Circle cannot purchase or pin stock");
            yield return TapMenuButton(GamepadButton.LeftShoulder);
            Check(MetaProgress.Pin == "LuckyPenny", "the secondary button pins the hovered item");
            yield return Capture("ShopControllerButtons");
            yield return TapMenuButton(GamepadButton.South);
            Check(MetaProgress.Coins < balance && !MetaProgress.Stock.Contains("LuckyPenny"), "Cross buys the hovered item once");
        }
        craft.position = new Vector3(-12.4f, 3.35f, craft.position.z);
        yield return new WaitForSeconds(.5f);
        yield return TapMenuButton(GamepadButton.South);
        Check(HangarHub.HoldingShip, "Cross takes control of the Fleet console");
        yield return TapMenuButton(GamepadButton.Start);
        yield return TapMenuButton(GamepadButton.East);
        Check(!Menu.GameplayPaused && HangarHub.HoldingShip, "Circle closes pause without releasing the console underneath");
        yield return TapMenuButton(GamepadButton.East);
        Check(!HangarHub.HoldingShip, "a second Circle press releases the console");
        float gateY = Camera.UpLimit + HangarHub.DeckExtension - .1f;
        craft.position = new Vector3(3.2f, gateY - 3, craft.position.z);
        yield return new WaitForSeconds(.8f);
        yield return Capture("WideLaunchGate");
        craft.position = new Vector3(3.2f, gateY, craft.position.z);
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
        CheckResultLettering();
        Check(MetaProgress.LastFlight.outcome == "Victory" && MetaProgress.CanFly(ShipFrame.Lancer), "Hell reward exit ends campaign and releases Lancer");
        yield return DismissResult(GamepadButton.South);
        while (GameOverState.Active || (hub = FindAnyObjectByType<HangarHub>()) == null || CloudTransition.Busy || HangarHub.HoldingShip)
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
        CheckResultLettering();
        Check(MetaProgress.LastFlight.outcome == "Lost" && MetaProgress.LastFlight.awards.Count == 1 && !MetaProgress.CanFly(ShipFrame.Ember), "loss files a new boss stamp and preserves hull lock");
        yield return DismissResult(GamepadButton.East);
    }

}
#endif
