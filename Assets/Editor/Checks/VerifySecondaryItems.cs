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
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[InitializeOnLoad]
public static class VerifySecondaryItems
{
    public const string Folder = "ArtSource/SecondaryItemsReview";
    const string Key = "SecondaryItemsReview";
    static double deadline;
    static VerifySecondaryItems()
    {
        EditorApplication.playModeStateChanged += state => {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
            deadline = EditorApplication.timeSinceStartup + 240;
            new GameObject("Secondary item review").AddComponent<SecondaryItemsReview>();
        };
        EditorApplication.update += () => {
            if (deadline == 0 || !SessionState.GetBool(Key, false)) return;
            string path = Folder + "/Verification.txt";
            if (!File.Exists(path) && EditorApplication.timeSinceStartup < deadline) return;
            SessionState.SetBool(Key, false);
            if (!File.Exists(path)) File.WriteAllText(path, "FAIL timeout");
            CheckHarness.Exit(File.ReadAllText(path).StartsWith("PASS") ? 0 : 1);
        };
    }
    public static void Run()
    {
        if (!Application.dataPath.Replace('\\', '/').Contains("/Temp/DecisionHoverReview/"))
            throw new Exception("Use the isolated review project");
        Directory.CreateDirectory(Folder);
        File.Delete(Folder + "/Verification.txt");
        PlayerSettings.companyName = "ShootemupReview";
        PlayerSettings.productName = "SecondaryItemsReview";
        SessionState.SetBool(Key, true);
        SessionState.SetString(Key + "Focus", VerifyCombatRework.Argument("--secondary-focus", "all"));
        NativeGameView();
        EditorSceneManager.OpenScene("Assets/Scenes/L1.unity");
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/L1.unity");
        EditorApplication.isPlaying = true;
    }
    public static string Focus => SessionState.GetString(Key + "Focus", "all");
    public static void RenderModels()
    {
        if (!Application.dataPath.Replace('\\', '/').Contains("/Temp/DecisionHoverReview/"))
            throw new Exception("Use the isolated review project");
        Directory.CreateDirectory(Folder);
        EditorSceneManager.OpenScene("Assets/Scenes/L1.unity");
        var camera = new GameObject("Item review camera").AddComponent<UnityEngine.Camera>();
        camera.transform.position = new Vector3(1000, 0, -5);
        camera.orthographic = true; camera.orthographicSize = .62f;
        camera.cullingMask = 1 << 31;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.035f, .055f, .065f);
        var target = new RenderTexture(256, 256, 24);
        camera.targetTexture = target;
        string[] keys = { "SteelWashers", "ClockworkSpring", "RollerBearings", "Thermos", "HeatSink", "LuckyPenny",
            "PaperclipBox", "FireflyJar", "SaltShaker", "PocketMortar", "WindUpTeeth", "RibbonSpool", "SewingKit" };
        string only = VerifyCombatRework.Argument("--secondary-model", "");
        if (only.Length > 0) keys = keys.Where(key => key == only).ToArray();
        foreach (string key in keys)
        {
            var model = new GameObject(key) { layer = 31 };
            model.transform.position = new Vector3(1000, 0, 0);
            var mesh = IndustrialMeshFactory.ExpansionItem(key);
            model.AddComponent<MeshFilter>().sharedMesh = mesh;
            model.AddComponent<MeshRenderer>().sharedMaterial = EverydayItemMaterials.For(key);
            float span = Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.y, mesh.bounds.size.z);
            model.transform.localScale = Vector3.one * (.9f / span);
            for (int view = 0; view < 2; view++)
            {
                model.transform.rotation = Quaternion.Euler(-20, view == 0 ? 0 : 35, 0);
                camera.Render();
                var previous = RenderTexture.active; RenderTexture.active = target;
                var png = new Texture2D(256, 256, TextureFormat.RGB24, false);
                png.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); png.Apply();
                File.WriteAllBytes(Folder + "/Model-" + key + "-" + view + ".png", png.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(png); RenderTexture.active = previous;
            }
            UnityEngine.Object.DestroyImmediate(model);
        }
        File.WriteAllText(Folder + (only.Length == 0 ? "/ModelVerification.txt" : "/" + only + "Verification.txt"),
            "PASS\nFixed front and three-quarter views of " + string.Join(", ", keys) + " actual pickup meshes, rendered at 256x256. No gameplay run.\n");
        if (VerifyCombatRework.Argument("--secondary-output", "").Length > 0) Build();
        else EditorApplication.Exit(0);
    }
    static void NativeGameView()
    {
        var assembly = typeof(EditorWindow).Assembly;
        var view = EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
        var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
        var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var sizes = singleton.GetProperty("instance").GetValue(null);
        var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new object[] { 1 });
        var sizeType = assembly.GetType("UnityEditor.GameViewSize");
        var size = Activator.CreateInstance(sizeType, Enum.ToObject(assembly.GetType("UnityEditor.GameViewSizeType"), 1), 640, 480, "Secondary review");
        group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
        int count = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
        view.GetType().GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(view, count - 1);
        view.Show();
    }
    public static void Build()
    {
        if (!Application.dataPath.Replace('\\', '/').Contains("/Temp/DecisionHoverReview/") ||
            !File.ReadAllText(Folder + "/Verification.txt").StartsWith("PASS"))
            throw new Exception("Complete the isolated review first");
        PlayerSettings.companyName = "DefaultCompany";
        PlayerSettings.productName = "seup";
        string path = VerifyCombatRework.Argument("--secondary-output", "");
        if (!Path.IsPathRooted(path) || Path.GetExtension(path) != ".exe") throw new Exception("Absolute .exe path required");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            target = BuildTarget.StandaloneWindows64, locationPathName = path });
        bool ok = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
        File.WriteAllText(Folder + "/Build.txt", (ok ? "PASS" : "FAIL") + "\nWindows x64: " + path +
            "\nErrors=" + report.summary.totalErrors + "; warnings=" + report.summary.totalWarnings + "\n");
        EditorApplication.Exit(ok ? 0 : 1);
    }
}

public sealed class SecondaryItemsReview : MonoBehaviour
{
    static readonly string[] Stats = { "SteelWashers", "ClockworkSpring", "RollerBearings", "Thermos", "HeatSink", "LuckyPenny" };
    static readonly string[] Shots = { "PaperclipBox", "FireflyJar", "SaltShaker", "PocketMortar", "WindUpTeeth", "RibbonSpool", "SewingKit" };
    readonly List<string> checks = new List<string>(), errors = new List<string>();
    Shipcontroller ship;
    RunInventory inventory;
    Keyboard keyboard;
    void Awake() { Application.runInBackground = true; Application.logMessageReceived += Log; }
    void OnDestroy() { Application.logMessageReceived -= Log; }
    void Log(string message, string trace, LogType type)
    {
        if ((type == LogType.Error || type == LogType.Exception) && !trace.Contains("UnityEditor.Search")) errors.Add(message + "\n" + trace);
    }
    void Check(bool ok, string message)
    {
        if (!ok) throw new Exception(message);
        checks.Add(message);
        File.WriteAllLines(VerifySecondaryItems.Folder + "/Progress.txt", checks);
    }
    static bool Near(float a, float b) => Mathf.Abs(a - b) < .005f;
    void Press(params Key[] keys)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        InputSystem.Update();
    }
    IEnumerator Grant(string key)
    {
        var go = Instantiate(Resources.Load<GameObject>("Items/Item"), new Vector3(60, 60, 1), Quaternion.identity);
        var item = ItemCatalog.Get(key).Attach(go);
        item.SetRarity(ItemPool.RarityOf(key));
        yield return null;
        item.GrantTo(ship);
        yield return null;
    }
    RunInventory.Entry Last => inventory.Entries[inventory.Entries.Count - 1];
    void ClearRounds()
    {
        foreach (var b in FindObjectsByType<PlayerRound>()) b.Despawn();
    }
    IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();
        var texture = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(VerifySecondaryItems.Folder + "/" + name + ".png", texture.EncodeToPNG());
        Destroy(texture);
    }
    IEnumerator Start()
    {
        var stack = new Stack<IEnumerator>(); stack.Push(Run());
        while (stack.Count > 0)
        {
            object step = null; bool more = false; Exception failure = null;
            try { more = stack.Peek().MoveNext(); if (more) step = stack.Peek().Current; }
            catch (Exception e) { failure = e; }
            if (failure != null) { errors.Add(failure.ToString()); break; }
            if (!more) { stack.Pop(); continue; }
            if (step is IEnumerator nested) { stack.Push(nested); continue; }
            yield return step;
        }
        if (keyboard != null) { Press(); InputSystem.RemoveDevice(keyboard); }
        Time.timeScale = 1;
        File.WriteAllText(VerifySecondaryItems.Folder + "/Verification.txt", (errors.Count == 0 ? "PASS" : "FAIL") +
            "\n" + string.Join("\n", checks) + "\n" + string.Join("\n", errors));
    }
    IEnumerator Run()
    {
        float until = Time.realtimeSinceStartup + 45;
        while ((ship = FindAnyObjectByType<Shipcontroller>()) == null && Time.realtimeSinceStartup < until) yield return null;
        while (FindAnyObjectByType<WaveOrchestrer>() == null && Time.realtimeSinceStartup < until) yield return null;
        Check(ship != null, "isolated Plains player loaded");
        var flow = FindAnyObjectByType<WaveOrchestrer>();
        flow.enabled = false; flow.CancelInvoke(); flow.StopAllCoroutines();
        while (CloudTransition.Busy && Time.realtimeSinceStartup < until) yield return null;
        foreach (var shop in FindObjectsByType<DecisionShop>()) Destroy(shop.gameObject);
        foreach (var enemy in AEnemy.Live.ToArray()) if (enemy != null) Destroy(enemy.gameObject);
        foreach (var item in AItem.Available.ToArray()) if (item != null) Destroy(item.gameObject);
        foreach (var bullet in FindObjectsByType<AEnemyBullet>()) bullet.Vanish();
        yield return null;
        CombatDecision.End(); Time.timeScale = 1;
        GameRules.playerDisabled = false;
        ship.ObserverMode = true;
        ship.transform.position = new Vector3(0, Camera.DownLimit + 1.8f, -1);
        inventory = RunInventory.For(ship);
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        foreach (var device in InputSystem.devices.ToArray()) if (device is Keyboard || device is Gamepad) InputSystem.DisableDevice(device);
        keyboard = InputSystem.AddDevice<Keyboard>(); Press();
        foreach (var gun in ship.GetComponentsInChildren<AWeapon>()) gun.enabled = false;
        if (VerifySecondaryItems.Focus == "all" || VerifySecondaryItems.Focus == "stats") yield return StatChecks();
        if (VerifySecondaryItems.Focus == "all" || VerifySecondaryItems.Focus == "shots") yield return ShotChecks();
        if (VerifySecondaryItems.Focus == "all" || VerifySecondaryItems.Focus == "contacts" || VerifySecondaryItems.Focus == "remaining") yield return ContactChecks();
        if (VerifySecondaryItems.Focus == "all" || VerifySecondaryItems.Focus == "models") yield return Models();
        if (VerifySecondaryItems.Focus == "remaining") yield return Models();
        Check(errors.Count == 0, "no runtime errors");
    }
    IEnumerator StatChecks()
    {
        float damage = GameRules.playerShootDamage, rate = GameRules.playerShootSpeed, move = GameRules.playerMoveSpeed;
        float luck = GameRules.luck, energy = ship.maxMana;
        foreach (string key in Stats) yield return Grant(key);
        Check(Near(GameRules.playerShootDamage, damage + .25f) && Near(GameRules.playerShootSpeed, rate + .8f) &&
            Near(GameRules.playerMoveSpeed, move + .48f) && Near(GameRules.luck, luck + .05f), "four stat pickups apply their base increments");
        Check(Near(ship.maxMana, energy + 20) && Near(ship.mana, ship.maxMana), "Thermos adds and fills twenty energy");
        var thermos = inventory.Entries.First(e => e.key == "Thermos");
        var sink = inventory.Entries.First(e => e.key == "HeatSink");
        yield return Grant("Thermos"); yield return Grant("HeatSink");
        Check(Near(GameRules.activeEnergyCost, .81f), "Heat Sink copies stack multiplicatively");
        inventory.Remove(thermos); inventory.Remove(sink);
        Check(Near(ship.maxMana, energy + 20) && ship.mana <= ship.maxMana && Near(GameRules.activeEnergyCost, .9f),
            "trading older Thermos and Heat Sink preserves later copies and clamps the meter");
        var activeGo = new GameObject("energy check");
        var active = activeGo.AddComponent<PocketFan>(); active.s = ship; active.sgo = ship.gameObject;
        float cost = active.cost;
        float result = active.PressWithEnergyEfficiency(cost * .9f);
        Check(Near(result, 0) && Near(active.cost, cost), "discount permits an active with exactly reduced energy and restores its base cost");
        active.lastCD = 1000;
        Check(active.PressWithEnergyEfficiency(cost * .9f - .1f) == -1, "active still rejects insufficient discounted energy");
        Destroy(activeGo);
        foreach (var entry in inventory.Entries.Where(e => Stats.Contains(e.key)).ToArray()) inventory.Remove(entry);
        yield return null;
        Check(Near(GameRules.playerShootDamage, damage) && Near(GameRules.playerShootSpeed, rate) && Near(GameRules.playerMoveSpeed, move) &&
            Near(GameRules.luck, luck) && Near(ship.maxMana, energy) && Near(GameRules.activeEnergyCost, 1), "all six stat items trade back cleanly");
    }
    IEnumerator ShotChecks()
    {
        var slots = ship.GetComponent<ShipLoadout>().Weapons.ToArray();
        foreach (string key in Shots)
        {
            ClearRounds(); yield return Grant(key);
            var gun = ship.GetComponentsInChildren<SecondaryShot>().Single();
            float energy = ship.mana;
            int start = gun.Releases;
            Press(Key.Z); yield return new WaitForSeconds(1.05f);
            Check(gun.Releases - start >= Mathf.FloorToInt(gun.ReleasesPerSecond) && gun.Releases - start <= Mathf.CeilToInt(gun.ReleasesPerSecond * 1.05f) + 1,
                key + " fires at its authored cadence with primary held");
            var rounds = FindObjectsByType<PlayerRound>();
            Check(rounds.Length > 0 && rounds.All(r => r.generation > 0), key + " uses bounded secondary rounds without recursive procs");
            if (key == "SaltShaker") Check(rounds.Any(r => r.transform.up.y < 0), "Salt Shaker covers behind the ship");
            if (key == "PaperclipBox" || key == "SewingKit") Check(rounds.All(r => r.pierces == 1), key + " retains piercing");
            yield return Capture(key + "Wide");
            Press(); yield return null;
            int stopped = gun.Releases; yield return new WaitForSeconds(.2f);
            Check(gun.Releases == stopped && Near(ship.mana, energy), key + " stops on release and uses no active energy");
            ClearRounds(); Press(Key.X, Key.LeftShift); yield return new WaitForSeconds(.8f);
            Check(gun.Releases > stopped, key + " also fires with secondary held");
            rounds = FindObjectsByType<PlayerRound>();
            if (key == "RibbonSpool" || key == "SewingKit" || key == "PaperclipBox")
                Check(rounds.All(r => Mathf.Abs(r.transform.up.x) < .16f), key + " focus tightens the shot geometry");
            yield return Capture(key + "Focus");
            Time.timeScale = 0; int paused = gun.Releases;
            yield return new WaitForSecondsRealtime(.15f);
            Check(gun.Releases == paused, key + " respects pause");
            Time.timeScale = 1; CombatDecision.Begin();
            yield return new WaitForSeconds(.15f);
            Check(gun.Releases == paused, key + " respects decisions");
            CombatDecision.End(); Press();
            var entry = Last;
            yield return Grant(key);
            Check(ship.GetComponentsInChildren<SecondaryShot>().Length == 2, key + " duplicates have separate ownership");
            inventory.Remove(entry); yield return null;
            Check(ship.GetComponentsInChildren<SecondaryShot>().Length == 1, key + " trade removes exactly one battery");
            inventory.Remove(Last); yield return null;
            Check(ship.GetComponentsInChildren<SecondaryShot>().Length == 0, key + " last trade removes its option");
            Check(ship.GetComponent<ShipLoadout>().Weapons.SequenceEqual(slots), key + " preserves both weapon slots");
        }
    }
    IEnumerator ContactChecks()
    {
        ClearRounds();
        yield return Grant("SewingKit");
        var target = DistrictEnemy.Spawn(typeof(ScoutInterceptor), ship.transform.position + Vector3.up * 5);
        target.health = 100;
        typeof(DistrictEnemy).GetField("nextAttack", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, 10000f);
        foreach (var mover in target.GetComponents<PatternMover>()) mover.enabled = false;
        yield return null;
        Press(Key.Z, Key.LeftShift); yield return new WaitForSeconds(1.2f);
        checks.Add("contact diagnostic: ship=" + ship.transform.position + ", scale=" + ship.transform.lossyScale +
            ", target=" + target.transform.position + ", rounds=" + PlayerRound.LiveCount + ", health=" + target.health);
        yield return Capture("Contact");
        Check(target.health < 100, "rapid option needles make actual native projectile hits");
        Press(); Destroy(target.gameObject);
        foreach (string key in Shots.Where(k => k != "SewingKit")) yield return Grant(key);
        Press(Key.Z); yield return new WaitForSeconds(2.5f);
        Check(PlayerRound.LiveCount <= PlayerRound.Limit - 50 && PlayerRound.CreatedCount <= PlayerRound.Limit,
            "all seven patterns stay within the existing pool and preserve primary capacity");
        yield return Capture("Combined");
        Press();
        foreach (var entry in inventory.Entries.Where(e => Shots.Contains(e.key)).ToArray()) inventory.Remove(entry);
        ClearRounds(); yield return null;
    }
    IEnumerator Models()
    {
        string[] kinds = Stats.Concat(Shots).ToArray();
        var studio = ItemPortraitStudio.Create(kinds);
        studio.enabled = false;
        yield return null;
        foreach (var model in studio.GetComponentsInChildren<MeshFilter>())
        {
            Check(model.sharedMesh.vertexCount > 30 && model.sharedMesh.bounds.size.z > .05f, model.name + " has volumetric modeled detail");
            model.transform.localRotation = Quaternion.Euler(-22, 24, 0);
        }
        yield return new WaitForEndOfFrame();
        var texture = (RenderTexture)studio.Atlas;
        var previous = RenderTexture.active; RenderTexture.active = texture;
        var png = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        png.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0); png.Apply();
        File.WriteAllBytes(VerifySecondaryItems.Folder + "/ModelAtlas.png", png.EncodeToPNG());
        RenderTexture.active = previous; Destroy(png); Destroy(studio.gameObject);
    }
}
#endif
