#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[InitializeOnLoad]
public static class VerifyOffensiveItems
{
    public const string Folder = "ArtSource/OffensiveItemsReview";
    const string Key = "OffensiveItemsReview.Running";
    static double deadline;
    static VerifyOffensiveItems()
    {
        EditorApplication.playModeStateChanged += state => {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
            deadline = EditorApplication.timeSinceStartup + 180;
            new GameObject("Offensive items verification").AddComponent<OffensiveItemsReview>();
        };
        EditorApplication.update += () => {
            if (deadline == 0 || !SessionState.GetBool(Key, false)) return;
            string report = Folder + "/Verification.txt";
            if (!File.Exists(report) && EditorApplication.timeSinceStartup < deadline) return;
            SessionState.SetBool(Key, false);
            if (!File.Exists(report)) File.WriteAllText(report, "FAIL: timeout");
            CheckHarness.Exit(File.ReadAllText(report).StartsWith("PASS") ? 0 : 1);
        };
    }
    public static void Run()
    {
        RequireIsolated();
        Directory.CreateDirectory(Folder);
        if (File.Exists(Folder + "/Verification.txt")) File.Delete(Folder + "/Verification.txt");
        PlayerSettings.companyName = "ShootemupReview";
        PlayerSettings.productName = "OffensiveItemsReview";
        SessionState.SetBool(Key, true);
        EditorSceneManager.OpenScene("Assets/Scenes/L1.unity");
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/L1.unity");
        CheckHarness.EnterPlayMode();
    }
    public static void RequireIsolated()
    {
        if (!Application.dataPath.Replace('\\', '/').Contains("/Build/OffensiveItemsReview/"))
            throw new Exception("Use the isolated offensive-item review copy");
    }
    public static void Build()
    {
        RequireIsolated();
        if (!File.ReadAllText(Folder + "/Verification.txt").StartsWith("PASS")) throw new Exception("Verification must pass first");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            target = BuildTarget.StandaloneWindows64,
            locationPathName = "Build/OffensiveItems/OffensiveItems.exe" });
        bool passed = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
        File.WriteAllText(Folder + "/Build.txt", (passed ? "PASS" : "FAIL") + "\nErrors=" + report.summary.totalErrors + "; warnings=" + report.summary.totalWarnings);
        EditorApplication.Exit(passed ? 0 : 1);
    }
}

public sealed class OffensiveItemsReview : MonoBehaviour
{
    readonly List<string> checks = new List<string>(), errors = new List<string>();
    Shipcontroller ship;
    ShipLoadout loadout;
    Keyboard keyboard;
    OffensiveActiveItem active;
    readonly List<OffensiveReviewTarget> targets = new List<OffensiveReviewTarget>();
    static readonly string[] Keys = { "RocketBouquet", "Redline", "NailBomb", "CanOpener", "FirecrackerBelt", "CarbonPaper", "Thunderhead", "DemolitionPlunger" };
    void Awake() { Application.runInBackground = true; Application.logMessageReceived += Log; }
    void OnDestroy() => Application.logMessageReceived -= Log;
    void Log(string message, string trace, LogType kind)
    {
        if (trace.Contains("UnityEditor.Search.SearchDatabase"))
        {
            File.AppendAllText(VerifyOffensiveItems.Folder + "/EditorWarnings.txt", message + "\n" + trace + "\n");
            return; // Unity's editor-only first-import search index; retained separately from runtime results.
        }
        if (kind == LogType.Error || kind == LogType.Exception) errors.Add(message + "\n" + trace);
    }
    void Check(bool good, string message)
    {
        if (!good) throw new Exception(message);
        checks.Add(message);
        File.WriteAllLines(VerifyOffensiveItems.Folder + "/Progress.txt", checks);
    }
    void Press(params Key[] keys)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        InputSystem.Update();
    }
    void Clear()
    {
        foreach (var shot in FindObjectsByType<PlayerRound>()) shot.Despawn();
        foreach (var target in targets) if (target != null) Destroy(target.gameObject);
        targets.Clear();
    }
    OffensiveReviewTarget Target(float x, float ahead = 3)
    {
        var go = new GameObject("Moving damage target");
        go.layer = LayerMask.NameToLayer("Enemy");
        if (go.layer < 0) go.layer = 0;
        go.transform.position = ship.transform.position + new Vector3(x, ahead, 0);
        go.AddComponent<BoxCollider2D>().size = new Vector2(1.2f, 1);
        var target = go.AddComponent<OffensiveReviewTarget>();
        target.health = 10000;
        var mesh = new GameObject("Target hull"); mesh.transform.SetParent(go.transform, false);
        mesh.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.CombatHull(0);
        mesh.AddComponent<MeshRenderer>().sharedMaterial = CombatMaterials.Fleet;
        mesh.transform.localScale = Vector3.one * .55f;
        targets.Add(target);
        return target;
    }
    IEnumerator Equip(string key)
    {
        Press();
        foreach (var gun in loadout.Weapons) if (gun != null) gun.StopFiring();
        Clear();
        var go = Instantiate(Resources.Load<GameObject>("Items/Item"), new Vector3(60, 60, 1), Quaternion.identity);
        active = (OffensiveActiveItem)ItemCatalog.Get(key).Attach(go);
        active.SetRarity(ItemPool.RarityOf(key));
        yield return null;
        active.GrantTo(ship, 0);
        yield return null;
        Check(loadout.Actives[0] == active, key + " equips through the real active slot");
        Check(active.OnPress(active.cost - .01f) == -1 && active.Cast == null, key + " rejects insufficient energy without casting");
        float paid = active.PressWithEnergyEfficiency(100);
        Check(Mathf.Abs(paid - (100 - active.cost)) < .001f && active.Cast != null, key + " spends energy once and starts its cast");
    }
    void Capture(string name)
    {
        var camera = UnityEngine.Camera.main;
        if (camera == null) return;
        var savedTarget = camera.targetTexture;
        var target = RenderTexture.GetTemporary(800, 600, 24);
        camera.targetTexture = target;
        camera.Render();
        var previous = RenderTexture.active;
        RenderTexture.active = target;
        var png = new Texture2D(800, 600, TextureFormat.RGB24, false);
        png.ReadPixels(new Rect(0, 0, 800, 600), 0, 0); png.Apply();
        File.WriteAllBytes(VerifyOffensiveItems.Folder + "/" + name + ".png", png.EncodeToPNG());
        Destroy(png); RenderTexture.active = previous; camera.targetTexture = savedTarget;
        RenderTexture.ReleaseTemporary(target);
    }
    IEnumerator Start()
    {
        var stack = new Stack<IEnumerator>(); stack.Push(Run());
        while (stack.Count > 0)
        {
            object step = null; bool more = false; Exception failure = null;
            try { more = stack.Peek().MoveNext(); if (more) step = stack.Peek().Current; }
            catch (Exception exception) { failure = exception; }
            if (failure != null) { errors.Add(failure.ToString()); break; }
            if (!more) { stack.Pop(); continue; }
            if (step is IEnumerator nested) { stack.Push(nested); continue; }
            yield return step;
        }
        if (keyboard != null) { Press(); InputSystem.RemoveDevice(keyboard); }
        Time.timeScale = 1;
        File.WriteAllText(VerifyOffensiveItems.Folder + "/Verification.txt", (errors.Count == 0 ? "PASS" : "FAIL") + "\n" +
            string.Join("\n", checks) + "\n" + string.Join("\n", errors));
    }
    IEnumerator Run()
    {
        MetaProgress.ReviewSaveDirectory = Path.GetFullPath("UserSettings/OffensiveSave");
        float until = Time.realtimeSinceStartup + 60;
        while ((ship = FindAnyObjectByType<Shipcontroller>()) == null && Time.realtimeSinceStartup < until) yield return null;
        while (FindAnyObjectByType<WaveOrchestrer>() == null && Time.realtimeSinceStartup < until) yield return null;
        Check(ship != null, "isolated Plains scene loaded");
        var flow = FindAnyObjectByType<WaveOrchestrer>();
        flow.enabled = false; flow.CancelInvoke(); flow.StopAllCoroutines();
        while (CloudTransition.Busy && Time.realtimeSinceStartup < until) yield return null;
        foreach (var decision in FindObjectsByType<DecisionShop>()) Destroy(decision.gameObject);
        foreach (var enemy in AEnemy.Live.ToArray()) if (enemy != null) Destroy(enemy.gameObject);
        foreach (var item in AItem.Available.ToArray()) if (item != null) Destroy(item.gameObject);
        foreach (var bullet in FindObjectsByType<AEnemyBullet>()) bullet.Vanish();
        yield return null;
        CombatDecision.End(); Time.timeScale = 1; GameRules.playerDisabled = false;
        ship.ObserverMode = true;
        ship.transform.position = new Vector3(0, Camera.DownLimit + 1.8f, -1);
        loadout = ship.GetComponent<ShipLoadout>();
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        foreach (var device in InputSystem.devices.ToArray()) if (device is Keyboard || device is Gamepad) InputSystem.DisableDevice(device);
        keyboard = InputSystem.AddDevice<Keyboard>(); Press();
        Check(Keys.All(key => ItemCatalog.Get(key).InRewardPool), "all eight items registered for progression and reward pools");
        yield return PlayerRound.Prepare();
        int warmed = PlayerRound.CreatedCount;

        bool carbonOnly = Environment.GetCommandLineArgs().Contains("-offensiveCarbon");
        bool followup = carbonOnly || Environment.GetCommandLineArgs().Contains("-offensiveFollowup");
        if (!followup)
        {
        yield return Equip("RocketBouquet");
        yield return new WaitForSeconds(.82f);
        Check(FindObjectsByType<PlayerRound>().Length == 12, "Rocket Bouquet releases exactly twelve pooled missiles");
        Capture("Flight-RocketBouquet");
        Clear(); yield return null;
        var guided = Target(1.3f);
        active.lastCD = 1000; active.OnPress(100);
        yield return new WaitForSeconds(1.8f);
        Check(guided.health < 10000, "bouquet missiles hit an offset moving target through native contacts");

        yield return Equip("FirecrackerBelt");
        yield return new WaitForSeconds(.35f);
        Check(FindObjectsByType<PlayerRound>().All(round => Mathf.Abs(round.transform.up.x) < .001f), "Firecracker barrage stays straight");
        Capture("Flight-FirecrackerBelt");
        yield return new WaitForSeconds(.95f);
        Check(active.Cast == null, "Firecracker barrage finishes after eighteen releases");

        yield return Equip("NailBomb");
        yield return new WaitForSeconds(.3f);
        Check(active.OnPress(0) == 0, "Nail Bomb can detonate with no remaining energy before cooldown");
        yield return null;
        var nails = FindObjectsByType<PlayerRound>();
        Check(nails.Length == 18 && nails.All(round => round.pierces == 3), "Nail Bomb emits eighteen piercing nails");
        Capture("Flight-NailBomb");
        Check(active.OnPress(100) == -2, "detonating does not bypass the next bomb's cooldown");
        }

        if (!carbonOnly)
        {
        yield return Equip("CanOpener");
        var drilled = Target(0, 2.8f);
        yield return new WaitForSeconds(.65f);
        Check(drilled.health < 10000, "Can Opener contacts and grinds a moving target");
        Capture("Flight-CanOpener");
        float frozenAge = active.Cast.Age, frozenHealth = drilled.health;
        CombatDecision.Begin(); yield return new WaitForSeconds(.25f);
        Check(Mathf.Approximately(frozenAge, active.Cast.Age) && drilled.health == frozenHealth, "offensive duration and damage freeze during decisions");
        CombatDecision.End();
        drilled.Gated = true;
        yield return new WaitForSeconds(.25f);
        Check(drilled.health == frozenHealth && active.Cast == null, "drill releases an invulnerable target without bypassing its gate");
        }

        if (!followup)
        {
        yield return Equip("Thunderhead");
        var coilTarget = Target(0, 3.7f);
        yield return new WaitForSeconds(.55f);
        Check(Mathf.Abs((10000 - coilTarget.health) - GameRules.playerShootDamage * 2.7f) < .02f, "a lone Thunderhead target takes all three links of one pulse");
        Capture("Flight-Thunderhead");
        coilTarget.Gated = true; float gatedHealth = coilTarget.health;
        yield return new WaitForSeconds(.35f);
        Check(coilTarget.health == gatedHealth, "coil respects invulnerable targets");

        yield return Equip("DemolitionPlunger");
        var marked = Target(0, 2.6f);
        Press(Key.Z); yield return new WaitForSeconds(.65f); Press();
        var demolition = (DemolitionMarks)active.Cast;
        Check(demolition.MarkCount == 1, "actual weapon contacts plant demolition charges");
        ClearPlayerShots();
        float beforeBoom = marked.health;
        Check(active.OnPress(0) == 0 && marked.health < beforeBoom, "plunger re-press detonates planted charges without another energy cost");
        Capture("Flight-DemolitionPlunger");
        }

        if (!carbonOnly)
        {
        loadout.EquipWeapon(nameof(RailDriver), 1); yield return null;
        yield return Equip("Redline");
        float baseRate = GameRules.playerShootSpeed;
        yield return new WaitForSeconds(.55f);
        Check(loadout.Weapons.All(gun => gun.isShootingPrimary || gun.isShootingSecondary), "Redline fires both equipped guns without held fire");
        Check(FindObjectsByType<PlayerRound>().Length >= 16, "Redline accelerates actual projectile output");
        Capture("Flight-Redline");
        loadout.EquipWeapon(nameof(WindUpCannon), 0); loadout.EquipWeapon(nameof(BeamEmitter), 1);
        bool releasedCharge = false;
        float observeUntil = Time.time + 1.3f;
        while (Time.time < observeUntil)
        {
            releasedCharge |= FindObjectsByType<PlayerRound>().Any(round => round.delivery == PlayerDelivery.Needle);
            yield return null;
        }
        Check(releasedCharge, "Redline automatically releases fully charged cannon shots after weapon replacement");
        Check(FindObjectsByType<BeamRibbon>().Length > 0, "Redline also drives continuous beam weapons");
        yield return new WaitForSeconds(2.4f);
        Check(RedlineDrive.Current == null && GameRules.playerShootSpeed == baseRate && loadout.Weapons.All(gun => !gun.isShootingPrimary && !gun.isShootingSecondary), "Redline restores controls and leaves permanent rate untouched");
        }
        else
        {
            loadout.EquipWeapon(nameof(WindUpCannon), 0); loadout.EquipWeapon(nameof(BeamEmitter), 1);
            yield return null;
        }

        yield return Equip("CarbonPaper");
        Press(Key.X); yield return new WaitForSeconds(.45f);
        var copies = FindObjectsByType<AWeapon>().Where(gun => gun.IsCarbonCopy).ToArray();
        Check(copies.Length == 4 && copies.Count(gun => gun is WindUpCannon) == 2 && copies.Count(gun => gun is BeamEmitter) == 2, "Carbon Paper makes two copies of both actual equipped weapon types");
        Check(FindObjectsByType<BeamRibbon>().Length >= 6, "Carbon Paper copies attached continuous beams");
        Capture("Flight-CarbonPaper");
        Press(); yield return null;
        loadout.EquipWeapon(nameof(ScatterCannon), 0); yield return new WaitForSeconds(.2f);
        Check(FindObjectsByType<AWeapon>().Count(gun => gun.IsCarbonCopy && gun is ScatterCannon) == 2, "Carbon copies follow equipment replacement and remove old guns");
        Press(Key.Z); yield return new WaitForSeconds(.4f);
        Check(FindObjectsByType<PlayerRound>().Length > 0, "copied scatter guns emit their actual projectile pattern");
        Press(); yield return new WaitForSeconds(4.2f);
        Check(CarbonCopies.Current == null && !FindObjectsByType<AWeapon>().Any(gun => gun.IsCarbonCopy), "Carbon Paper removes copies and their weapons at expiry");
        Check(PlayerRound.CreatedCount == warmed && PlayerRound.LiveCount <= PlayerRound.Limit, "all new projectile attacks reuse the 360-round pool");
        Clear();
        Check(errors.Count == 0, "no runtime errors in the combined active-item review");
    }
    void ClearPlayerShots() { foreach (var round in FindObjectsByType<PlayerRound>()) round.Despawn(); }
}

public sealed class OffensiveReviewTarget : AEnemy
{
    public bool Gated;
    Vector3 start;
    protected override void Start() { start = transform.position; }
    void Update() { transform.position = start + Vector3.right * Mathf.Sin(Time.time * 1.8f) * .15f; }
    public override bool CanBeTargeted => !Gated && health > 0;
    public override bool CanReceiveDamage => !Gated && health > 0;
    public override void damage(float amount) { if (!Gated) health -= amount; }
}
#endif
