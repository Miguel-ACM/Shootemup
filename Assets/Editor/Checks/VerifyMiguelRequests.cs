#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class VerifyMiguelRequests
{
    const string Key = "MiguelRequestsReview";
    static double deadline;
    static VerifyMiguelRequests()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false))
            {
                new GameObject("Miguel requests review").AddComponent<MiguelRequestsReview>();
                deadline = EditorApplication.timeSinceStartup + 360;
            }
        };
        EditorApplication.update += () =>
        {
            if (deadline == 0)
                return;
            string report = MiguelRequestsReview.Folder + "Verification.txt";
            if (File.Exists(report))
            {
                deadline = 0;
                SessionState.SetBool(Key, false);
                CheckHarness.Exit(File.ReadAllText(report).StartsWith("PASS") ? 0 : 1);
            }
            else if (EditorApplication.timeSinceStartup > deadline)
            {
                File.WriteAllText(report, "FAIL timed out");
                CheckHarness.Exit(1);
            }
        };
    }

    public static void Run()
    {
        Begin("all");
    }

    public static void Recheck()
    {
        Begin("recheck");
    }

    public static void SnowOnly()
    {
        Begin("snow");
    }

    public static void MenuOnly()
    {
        Begin("menu");
    }

    static void Begin(string scope)
    {
        Directory.CreateDirectory(MiguelRequestsReview.Folder);
        File.Delete(MiguelRequestsReview.Folder + "Verification.txt");
        SessionState.SetString(Key + "Scope", scope);
        SessionState.SetBool(Key, true);
        EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Show();
        CheckHarness.EnterPlayMode();
    }
}

public sealed class MiguelRequestsReview : MonoBehaviour
{
    public const string Folder = "ArtSource/MiguelRequestsReview/";
    readonly List<string> checks = new List<string>(), errors = new List<string>();
    Keyboard keyboard;
    Shipcontroller ship;
    static readonly BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance;
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Application.runInBackground = true;
        Application.logMessageReceived += Log;
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= Log;
    }

    void Log(string message, string trace, LogType type)
    {
        if ((type == LogType.Error || type == LogType.Exception) && !trace.Contains("UnityEditor.Search"))
            errors.Add(message + "\n" + trace);
    }

    void Check(bool okay, string message)
    {
        if (!okay)
            throw new Exception(message);
        checks.Add("PASS " + message);
        File.WriteAllLines(Folder + "Progress.txt", checks);
    }

    void Press(params Key[] keys)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        InputSystem.Update();
    }

    static void Set(object target, string field, object value)
    {
        Type type = target.GetType();
        while (type != null)
        {
            var found = type.GetField(field, Hidden);
            if (found != null)
            {
                found.SetValue(target, value);
                return;
            }
            type = type.BaseType;
        }
        throw new Exception("No field " + field);
    }

    static void Clear()
    {
        CombatRound.ClearAll();
        foreach (var shot in FindObjectsByType<ABullet>())
            Destroy(shot.gameObject);
        foreach (var blast in FindObjectsByType<ChargeBlast>())
            Destroy(blast.gameObject);
        foreach (var enemy in AEnemy.Live.ToArray())
            if (enemy != null)
                Destroy(enemy.gameObject);
        foreach (var item in FindObjectsByType<AItem>())
            if (!item.Collected)
                Destroy(item.gameObject);
        foreach (var pickup in FindObjectsByType<APickable>())
            Destroy(pickup.gameObject);
    }

    DistrictEnemy Target(Vector3 position)
    {
        var target = DistrictEnemy.Spawn(typeof(ScoutInterceptor), position);
        target.health = 10000;
        Set(target, "nextAttack", 10000f);
        foreach (var mover in target.GetComponents<PatternMover>())
            mover.enabled = false;
        return target;
    }

    IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();
        var shot = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Folder + name + ".png", shot.EncodeToPNG());
        Destroy(shot);
    }

    IEnumerator Grant(Type type, int slot = 0)
    {
        var go = Instantiate(Resources.Load<GameObject>("Items/Item"), new Vector3(100, 100, 1), Quaternion.identity);
        var item = (AItem)go.AddComponent(type);
        yield return null;
        item.GrantTo(ship, slot);
        yield return null;
    }

    IEnumerator Start()
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(Run());
        while (stack.Count > 0)
        {
            bool more = false;
            object step = null;
            Exception failure = null;
            try
            {
                more = stack.Peek().MoveNext();
                if (more)
                    step = stack.Peek().Current;
            }
            catch (Exception exception) { failure = exception; }
            if (failure != null)
            {
                errors.Add(failure.ToString());
                break;
            }
            if (!more)
            {
                stack.Pop();
                continue;
            }
            if (step is IEnumerator nested)
            {
                stack.Push(nested);
                continue;
            }
            yield return step;
        }
        if (keyboard != null)
        {
            Press();
            InputSystem.RemoveDevice(keyboard);
        }
        Time.timeScale = 1;
        File.WriteAllText(Folder + "Verification.txt", (errors.Count == 0 ? "PASS" : "FAIL") + "\n" + string.Join("\n", checks) + "\n" + string.Join("\n", errors));
    }

    IEnumerator Run()
    {
        yield return new WaitForSecondsRealtime(.7f);
        var start = FindAnyObjectByType<NewGame>();
        Check(start != null, "title has New Game");
        start.OnAccept();
        yield return null;
        Check(FindObjectsByType<DifficultyOption>().Length == 3 && !CloudTransition.Busy, "New Game opens Easy/Hard/Back without starting the run");
        Check(FindObjectsByType<DifficultyOption>().All(option => option.GetComponentInChildren<TMPro.TextMeshProUGUI>().fontSize >= 36), "difficulty labels remain readable on the title canvas");
        yield return Capture("Difficulty");
        start.Choose(2);
        yield return null;
        Check(start.gameObject.activeInHierarchy && FindObjectsByType<DifficultyOption>().Length == 0, "Back restores the title choices");
        if (SessionState.GetString("MiguelRequestsReviewScope", "all") == "menu")
            yield break;
        start.OnAccept();
        yield return null;
        start.Choose(0);
        while (CloudTransition.Busy || SceneManager.GetActiveScene().name != "L1")
            yield return null;
        yield return null;
        ship = FindAnyObjectByType<Shipcontroller>();
        ship.ObserverMode = true;
        ship.transform.position = new Vector3(0, -4, -1);
        var waves = FindAnyObjectByType<WaveOrchestrer>();
        waves.enabled = false;
        waves.CancelInvoke();
        waves.StopAllCoroutines();
        foreach (var shop in FindObjectsByType<DecisionShop>())
            shop.Abort();
        CombatDecision.End();
        Time.timeScale = 1;
        Clear();
        yield return null;
        Check(Difficulty.Easy && Mathf.Abs(GameRules.enemyDamage - .65f) < .001f && Mathf.Abs(LevelRun.EnemyShotSpeedScale - .8f) < .001f, "Easy applies lower incoming damage and bullet speed");
        LevelRun.Advance(LevelId.Snow);
        Check(Difficulty.Easy && Mathf.Abs(LevelRun.EnemyHealthScale - 1.16f * .8f) < .001f, "difficulty survives district advance and preserves tier health factors");
        GameRules.StartNewRun();
        LevelRun.Reset(LevelId.Fields, 7921);
        Check(!Difficulty.Easy && LevelRun.EnemyTempoScale == 1 && LevelRun.EnemyHealthScale == 1 && GameRules.enemyDamage == 1, "Hard preserves the previous baseline and new runs clear Easy");
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        foreach (var device in InputSystem.devices.ToArray())
            if (device is Keyboard || device is Gamepad)
                InputSystem.DisableDevice(device);
        keyboard = InputSystem.AddDevice<Keyboard>();
        bool all = SessionState.GetString("MiguelRequestsReviewScope", "all") == "all";
        if (all)
            yield return Catalog();
        if (all)
            yield return Weapons();
        else if (SessionState.GetString("MiguelRequestsReviewScope", "all") == "recheck")
            yield return ResumeWeapons();
        yield return Snow();
    }

    IEnumerator Weapons()
    {
        yield return Grant(typeof(WindUpCannonItem));
        var cannon = (WindUpCannon)ship.GetComponent<ShipLoadout>().Weapons[0];
        for (int i = 0; i < 4; i++)
        {
            Press(Key.Z);
            yield return null;
            Check(cannon.Charging, "rapid press " + i + " starts charging");
            Press();
            Check(!cannon.Charging, "release " + i + " fires immediately in the input callback");
        }
        Check(FindObjectsByType<PlayerRound>().Length == GameRules.playerShootNumber * 4, "four rapid taps produce four volleys without cooldown");
        Clear();
        yield return null;
        GameRules.playerShootSpeed = 20;
        Press(Key.Z);
        yield return new WaitForSeconds(.64f);
        Check(cannon.Level == 3 && Mathf.Abs(cannon.SecondsToFullCharge - .6f) < .001f, "double attack speed halves the time to full charge");
        Press();
        yield return null;
        Clear();
        yield return null;
        GameRules.playerShootSpeed = 10;
        var target = Target(new Vector3(0, -1, 1));
        yield return null;
        Press(Key.Z);
        yield return new WaitForSeconds(1.24f);
        Press();
        yield return new WaitForSeconds(.25f);
        Check(FindObjectsByType<ChargeBlast>().Length > 0, "charged impacts create lingering explosions");
        float health = target.health;
        yield return Capture("ChargeBlast");
        yield return new WaitForSeconds(.35f);
        Check(target.health < health - 2, "lingering explosions continue dealing damage after impact");
        Clear();
        yield return null;
        yield return Grant(typeof(BeamEmitterItem));
        target = Target(new Vector3(0, 1, 1));
        yield return null;
        Press(Key.Z);
        yield return new WaitForSeconds(.5f);
        health = target.health;
        yield return new WaitForSeconds(1.4f);
        Check(target.health < health - 15, "held beam keeps dealing damage beyond the old burst duration");
        yield return Capture("ContinuousLaser");
        yield return CurvedBeamAndRemaining(target);
    }

    IEnumerator ResumeWeapons()
    {
        yield return Grant(typeof(BeamEmitterItem));
        var target = Target(new Vector3(0, 1, 1));
        yield return null;
        Press(Key.Z);
        yield return new WaitForSeconds(.4f);
        yield return CurvedBeamAndRemaining(target);
    }

    IEnumerator CurvedBeamAndRemaining(DistrictEnemy target)
    {
        target.transform.position = new Vector3(2, -1, 1);
        GameRules.playerShootFollowness = 3;
        float health = target.health;
        yield return new WaitForSeconds(.8f);
        var beam = ship.GetComponent<ShipLoadout>().Weapons[0].GetComponentsInChildren<BeamRibbon>()[0];
        var lines = beam.GetComponentsInChildren<LineRenderer>();
        Check(lines.Any(line => line.positionCount == 41 && Enumerable.Range(0, 41).Any(i => line.GetPosition(i).x > .6f)), "Telekinesis visibly bends the continuous beam");
        Check(target.health < health, "beam damage follows its curved path to an off-axis target");
        yield return Capture("TelekineticLaser");
        Press();
        yield return new WaitForSeconds(.24f);
        Check(ship.GetComponentsInChildren<BeamRibbon>().Length == 0, "beam cools down and cleans up after release");
        GameRules.playerShootFollowness = 0;
        Clear();
        yield return null;
        var station = (SpaceCitadelBoss)DistrictEnemy.Spawn(typeof(SpaceCitadelBoss), new Vector3(0, 4, 1));
        Set(station, "nextAttack", 10000f);
        yield return null;
        health = station.Parts[4].health;
        Press(Key.Z);
        yield return new WaitForSeconds(.6f);
        Press();
        Check(station.Parts[4].health == health && !station.Parts[4].CanBeTargeted, "continuous beam respects the locked station reactor");
        yield return new WaitForSeconds(.24f);
        Clear();
        yield return null;
        target = Target(new Vector3(2, 1, 1));
        var drone = TrailDrone.Attach(ship, true);
        yield return new WaitForSeconds(.95f);
        Check((Vector2)typeof(TrailDrone).GetField("heading", Hidden).GetValue(drone) == Vector2.up, "laser drone commits straight upward despite an off-axis target");
        yield return Capture("DroneLaser");
        Destroy(drone.gameObject);
        Clear();
        yield return null;
        ship.transform.position = new Vector3(0, 0, -1);
        yield return Grant(typeof(BackdraftItem));
        target = Target(new Vector3(0, -2, 1));
        health = target.health;
        Press(Key.Z);
        yield return new WaitForSeconds(.27f);
        Check(FindObjectsByType<PlayerRound>().Any(shot => shot.delivery == PlayerDelivery.Flame && shot.transform.position.y < -.5f), "Backdraft emits flame below the player");
        Check(target.health < health && target.GetComponent<BurningHull>() != null, "rear flame damages and burns a target behind the ship");
        yield return Capture("Backdraft");
        Press();
        Clear();
        yield return null;
        yield return Grant(typeof(DepthChargeItem));
        Press(Key.Z);
        yield return new WaitForSeconds(.25f);
        Press();
        Check(FindObjectsByType<PlayerRound>().Any(shot => shot.delivery == PlayerDelivery.Shell && shot.transform.position.y < -.5f), "Depth Charge drops braking shells behind the player");
        yield return Capture("DepthCharge");
        yield return new WaitForSeconds(1);
        Check(FindObjectsByType<PlayerRound>().Any(shot => shot.delivery == PlayerDelivery.Pulse && shot.generation == 1), "rear shells release their fragmentation fan");
        Clear();
        yield return null;
        yield return Grant(typeof(BeamEmitterItem), 1);
        var itemObject = Instantiate(Resources.Load<GameObject>("Items/Item"), ship.transform.position, Quaternion.identity);
        var item = itemObject.AddComponent<BackdraftItem>();
        yield return null;
        yield return null;
        var dialog = FindAnyObjectByType<Dialog>();
        dialog.ShowItemDesc("Backdraft", "", item.GetData()[2], "rare");
        yield return new WaitForSeconds(.2f);
        var loadout = ship.GetComponent<ShipLoadout>();
        Check(loadout.HoveredItem == item, "full weapon slots require an explicit replacement on close hover");
        var badge = dialog.Panel.Find("Replacement controls above item card") as RectTransform;
        Check(badge != null && badge.gameObject.activeInHierarchy && badge.anchoredPosition.y > 0, "replacement key sprites are attached above the description card");
        Check(dialog.Panel.anchoredPosition.y >= DecisionOverlay.SkipHeight, "description card is entirely above the skip zone");
        yield return Capture("ReplacementCard");
        dialog.Hide();
        Clear();
        yield return null;
    }

    IEnumerator Snow()
    {
        Press();
        ship.GetComponent<ShipLoadout>().Weapons[0].StopFiring();
        LevelRun.Reset(LevelId.Snow, 7921);
        ship.transform.position = new Vector3(0, -4, -1);
        var boss = (SnowSiegeBoss)DistrictEnemy.Spawn(typeof(SnowSiegeBoss), new Vector3(0, 4, 1));
        yield return null;
        float until = Time.time + 7;
        bool seen = false;
        int captures = 0;
        while (Time.time < until)
        {
            var emitters = boss.GetComponentsInChildren<MeshFilter>().Where(filter => filter.sharedMesh != null && filter.sharedMesh.name == "Snow deployable comb emitter").ToArray();
            if (emitters.Length >= 10)
            {
                seen = true;
                if (captures < 4)
                {
                    yield return Capture("SnowComb" + captures);
                    captures++;
                }
            }
            yield return new WaitForSeconds(.2f);
        }
        Check(seen, "Snow visibly deploys its physical comb emitters during the real firing score");
        var shadows = FindObjectsByType<BossShadowGraphic>();
        Check(shadows.Length > 0 && shadows.Any(shadow => shadow.gameObject.activeInHierarchy), "boss bottom shadow appears during the fight");
        var mini = DistrictEnemy.Spawn(typeof(SnowSiegeCraft), new Vector3(-2, 3, 1));
        Set(mini, "nextAttack", 10000f);
        yield return new WaitForSeconds(.25f);
        Check(FindObjectsByType<BossShadowGraphic>().Length >= 2, "miniboss receives its own bottom position shadow");
        yield return Capture("BossAndMinibossShadows");
        Destroy(mini.gameObject);
        var debris = PSXPolygonDebris.Spawn(boss.gameObject, true, true);
        Check(debris != null && (float)typeof(PSXPolygonDebris).GetField("lifetime", Hidden).GetValue(debris) == 5, "boss wreckage lifetime is five seconds from the shatter accent");
        debris.ReleasePrepared();
        Destroy(boss.gameObject);
        CombatRound.ClearAll();
        yield return new WaitForSeconds(3.1f);
        Check(debris != null, "boss debris survives beyond the old lifetime");
        yield return Capture("BossDebris3Seconds");
        yield return new WaitForSeconds(2.1f);
        Check(debris == null, "boss debris cleans up after five seconds");
        Clear();
    }

    IEnumerator Catalog()
    {
        var kinds = typeof(ItemPool).GetFields(BindingFlags.Static | BindingFlags.NonPublic).Where(field => field.GetValue(null) is string[]).SelectMany(field => (string[])field.GetValue(null)).Distinct().ToArray();
        Check(kinds.Contains("BackdraftItem") && kinds.Contains("DepthChargeItem"), "both rear weapons are available in reward and shop pools");
        for (int page = 0; page * 30 < kinds.Length; page++)
        {
            var studio = new GameObject("Equipment identity studio");
            studio.transform.position = new Vector3(1000, 0, 0);
            var cameraObject = new GameObject("Catalog camera");
            cameraObject.transform.SetParent(studio.transform, false);
            cameraObject.transform.localPosition = new Vector3(0, 0, -15);
            var camera = cameraObject.AddComponent<UnityEngine.Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.3f;
            camera.aspect = 1.5f;
            camera.farClipPlane = 25;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.035f, .05f, .06f);
            var texture = new RenderTexture(1800, 1200, 24);
            camera.targetTexture = texture;
            for (int i = 0; i < 30 && page * 30 + i < kinds.Length; i++)
            {
                string kind = kinds[page * 30 + i];
                var mesh = IndustrialMeshFactory.ExpansionItem(kind);
                Check(mesh != null && mesh.vertexCount > 20, kind + " has a modeled identity");
                var model = new GameObject(kind);
                model.transform.SetParent(studio.transform, false);
                model.transform.localPosition = new Vector3((i % 6 - 2.5f) * 2.35f, 4.1f - i / 6 * 2.05f, 0);
                model.transform.localRotation = Quaternion.Euler(-18, 22, 0);
                model.transform.localScale = Vector3.one * (1.25f / Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.y, mesh.bounds.size.z));
                model.AddComponent<MeshFilter>().sharedMesh = mesh;
                model.AddComponent<MeshRenderer>().sharedMaterial = EverydayItemMaterials.For(kind);
                var label = new GameObject("Label");
                label.transform.SetParent(studio.transform, false);
                label.transform.localPosition = model.transform.localPosition + new Vector3(0, -.83f, -1);
                var text = label.AddComponent<TMPro.TextMeshPro>();
                text.text = kind.Replace("Item", "");
                text.fontSize = 1.4f;
                text.alignment = TMPro.TextAlignmentOptions.Center;
                text.rectTransform.sizeDelta = new Vector2(2.3f, .4f);
            }
            yield return new WaitForEndOfFrame();
            var previous = RenderTexture.active;
            RenderTexture.active = texture;
            var image = new Texture2D(1800, 1200, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1800, 1200), 0, 0);
            image.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(Folder + "Equipment" + page + ".png", image.EncodeToPNG());
            Destroy(image);
            camera.targetTexture = null;
            Destroy(studio);
            texture.Release();
            Destroy(texture);
        }
    }
}
#endif
