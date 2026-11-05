#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class VerifyPauseItemBrowser
{
    const string Key = "PauseItemBrowserReview";
    public const string Folder = "ReviewOutput/PauseItemBrowser";
    static double deadline;
    static VerifyPauseItemBrowser()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
            SessionState.SetBool(Key, false);
            deadline = EditorApplication.timeSinceStartup + 180;
            new GameObject("Pause browser checks").AddComponent<PauseItemBrowserProbe>();
        };
        EditorApplication.update += () =>
        {
            if (deadline == 0 || EditorApplication.timeSinceStartup < deadline) return;
            File.WriteAllText(Folder + "/Verification.txt", "FAIL: timeout");
            EditorApplication.Exit(1);
        };
    }

    public static void Run()
    {
        if (!Application.isBatchMode || Application.dataPath.Replace('\\', '/') == "C:/Users/Miguel/Shootemup/Assets")
            throw new InvalidOperationException("Use an isolated headless Unity project.");
        Directory.CreateDirectory(Folder);
        PlayerSettings.companyName = "ShootemupReview";
        PlayerSettings.productName = "PauseItemBrowserReview";
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Key, true);
        EditorApplication.isPlaying = true;
    }
}

[DefaultExecutionOrder(32000)]
public sealed class PauseItemBrowserProbe : MonoBehaviour
{
    readonly List<string> checks = new List<string>();
    readonly List<string> errors = new List<string>();
    Transform panel;
    Keyboard keyboard;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Application.runInBackground = true;
        Application.targetFrameRate = 60;
        // The null graphics device cannot render URP particle batches.
        UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = null;
        QualitySettings.renderPipeline = null;
    }
    void Log(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception) errors.Add(message + "\n" + stack);
    }
    void OnDestroy() => Application.logMessageReceived -= Log;
    void LateUpdate()
    {
        // Keep cameras discoverable by gameplay, but never submit particle/line geometry
        // to Unity's null device, including cameras created by a pickup this frame.
        foreach (var camera in FindObjectsByType<UnityEngine.Camera>())
            camera.cullingMask = 0;
    }
    IEnumerator Start()
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(Run());
        Exception failure = null;
        while (stack.Count > 0)
        {
            bool more = false;
            object step = null;
            try { more = stack.Peek().MoveNext(); if (more) step = stack.Peek().Current; }
            catch (Exception error) { failure = error; }
            if (failure != null) break;
            if (!more) { stack.Pop(); continue; }
            if (step is IEnumerator nested) { stack.Push(nested); continue; }
            yield return step;
        }
        bool pass = failure == null && errors.Count == 0;
        File.WriteAllText(VerifyPauseItemBrowser.Folder + "/Verification.txt",
            (pass ? "PASS\n" : "FAIL\n") + string.Join("\n", checks) + "\n" + failure + "\n" + string.Join("\n", errors));
        EditorApplication.Exit(pass ? 0 : 1);
    }
    void Check(bool valid, string message)
    {
        checks.Add((valid ? "PASS " : "FAIL ") + message);
        File.WriteAllLines(VerifyPauseItemBrowser.Folder + "/Progress.txt", checks);
        if (!valid) throw new Exception(message);
    }
    Button Button(string name) => panel.GetComponentsInChildren<Button>().Single(b => b.name == name);
    TMP_Dropdown Dropdown(string name) => panel.GetComponentsInChildren<TMP_Dropdown>().Single(d => d.name == name);
    Button[] Items() => panel.Find("Items/Catalogue").GetComponentsInChildren<Button>();
    IEnumerator Click(string name)
    {
        Button(name).onClick.Invoke();
        yield return null;
        yield return null;
    }
    IEnumerator Escape()
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
        yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return null;
    }
    IEnumerator Pointer(string name, PointerEventData.InputButton button)
    {
        ExecuteEvents.Execute(Button(name).gameObject, new PointerEventData(EventSystem.current) { button = button }, ExecuteEvents.pointerClickHandler);
        yield return null;
        yield return null;
    }
    IEnumerator Run()
    {
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        keyboard = InputSystem.AddDevice<Keyboard>();
        GameRules.StartNewRun();
        LevelRun.Reset(LevelId.Fields);
        yield return SceneManager.LoadSceneAsync("L1");
        yield return new WaitForSecondsRealtime(3);
        while (CloudTransition.Busy) yield return null;
        var ship = FindAnyObjectByType<Shipcontroller>();
        var menu = FindAnyObjectByType<Menu>();
        var browser = menu.GetComponentInChildren<DebugRunMenu>();
        // Limit error collection to the browser under test; the headless scene's
        // shader warmup and editor search indexing are outside this check.
        Application.logMessageReceived += Log;
        Check(browser != null && browser.InRun, "pause includes DEBUG ITEMS");
        menu.setMenuOpen(true);
        yield return null;
        menu.TryAccept(browser);
        yield return null;
        panel = GameObject.Find("Debug panel").transform;
        int total = Items().Length;
        Check(total >= ItemCatalog.All.Count, "catalogue includes every item, including pacts and special pickups");
        Check(Menu.GameplayPaused && Time.timeScale == 0 && menu.Suspended, "browser keeps combat paused");
        var search = panel.GetComponentInChildren<TMP_InputField>();
        search.text = "aNoThEr ArM";
        Check(Items().Length == 1 && Items()[0].name == "Another arm", "case-insensitive multiword name search");
        float shots = GameRules.playerShootNumber;
        yield return Click("Another arm");
        yield return Click("Another arm");
        Check(GameRules.playerShootNumber == shots + 2, "repeat passive grants apply immediately while paused");
        yield return Pointer("Another arm", PointerEventData.InputButton.Left);
        Check(GameRules.playerShootNumber == shots + 1, "left click removes exactly one passive copy and its stats");
        yield return Pointer("Another arm", PointerEventData.InputButton.Right);
        Check(GameRules.playerShootNumber == shots + 2, "right click gives a copy without removing it");
        search.text = "telekinesis bends";
        Check(Items().Any(b => b.name == "Beam Emitter"), "search matches effects");
        search.text = "no-such-item-xyz";
        Check(Items().Length == 0, "empty search result");
        yield return Click("RESET FILTERS");
        Dropdown("Type").value = 2;
        Check(Items().Length > 5 && Items().All(b => b.name != "Another arm"), "weapon category filter");
        Dropdown("Rarity").value = 3;
        Check(Items().Any(b => b.name == "Rail Driver"), "type and rarity filters combine");
        yield return Click("SLOT 2");
        yield return Click("Rail Driver");
        Check(ship.GetComponent<ShipLoadout>().Weapons[1] is RailDriver, "grant replaces selected weapon slot");
        yield return Click("RESET FILTERS");
        var inventory = RunInventory.For(ship);
        yield return Click("SLOT 1");
        yield return Click("Rail Driver");
        yield return Click("SLOT 2");
        var replaced = inventory.Entries.Single(e => e.key == nameof(RailDriverItem) && e.slot == 1);
        yield return Click("Scatter Cannon");
        Check(ship.GetComponent<ShipLoadout>().Weapons[0] is RailDriver && ship.GetComponent<ShipLoadout>().Weapons[1] is ScatterCannon &&
            inventory.Entries.Count(e => e.key == nameof(RailDriverItem)) == 1 && replaced.display == null,
            "replacement removes only the old slot's pickup and icon, preserving the other copy");
        yield return Pointer("Scatter Cannon", PointerEventData.InputButton.Left);
        Check(ship.GetComponent<ShipLoadout>().Weapons[1] == null && !inventory.Entries.Any(e => e.key == nameof(ScatterCannonItem)),
            "left click unequips a weapon and removes its inventory entry");
        yield return Pointer("Rail Driver", PointerEventData.InputButton.Right);
        int companions = ship.Companions.Count;
        yield return Click("Three Moons");
        Check(ship.Companions.Count == companions + 3, "companion pickup creates its full group");
        yield return Pointer("Three Moons", PointerEventData.InputButton.Left);
        Check(ship.Companions.Count == companions, "removing a companion item removes its group");
        float health = ship.maxHealth;
        yield return Click("Spare Plating");
        yield return Pointer("Spare Plating", PointerEventData.InputButton.Left);
        Check(ship.maxHealth == health, "removing hull equipment restores maximum health");
        float damage = GameRules.playerShootDamage, rate = GameRules.playerShootSpeed;
        yield return Click("Blood Price");
        yield return Pointer("Blood Price", PointerEventData.InputButton.Left);
        Check(Mathf.Approximately(GameRules.playerShootDamage, damage) && Mathf.Approximately(GameRules.playerShootSpeed, rate),
            "pact removal reverses both benefit and cost");
        var hull = ship.GetComponent<PolygonCollider2D>();
        Vector2 hullPoint = hull.GetPath(0)[0];
        yield return Click("Shrinker juice");
        yield return Pointer("Shrinker juice", PointerEventData.InputButton.Left);
        Check(Vector2.Distance(hull.GetPath(0)[0], hullPoint) < .00001f, "shrinker removal restores the original hitbox");
        yield return Click("SLOT 1");
        yield return Click("Stasis Well");
        yield return Click("SLOT 2");
        yield return Click("Mirror Burst");
        var loadout = ship.GetComponent<ShipLoadout>();
        Check(loadout.Actives[0] is GravityWell && loadout.Actives[1] is MirrorBurst, "active grants use both slots and initialize while paused");
        yield return Pointer("Mirror Burst", PointerEventData.InputButton.Left);
        Check(loadout.Actives[1] == null && !inventory.Entries.Any(e => e.key == nameof(MirrorBurst)), "left click removes an active from equipment and inventory");
        yield return Click("Mirror Burst");
        Dropdown("Collection").value = 1;
        Check(Items().Any(b => b.name == "Another arm") && Items().Any(b => b.name == "Rail Driver"), "collected filter follows inventory and equipment");
        Dropdown("Collection").value = 2;
        Check(!Items().Any(b => b.name == "Another arm"), "not-collected filter excludes granted items");
        yield return Click("RESET FILTERS");
        Dropdown("Order").value = 1;
        var names = Items().Select(b => b.name).ToArray();
        Check(names.SequenceEqual(names.OrderByDescending(n => n, StringComparer.OrdinalIgnoreCase)), "Z-A sorting reorders cards");
        Dropdown("Order").Show();
        yield return null;
        Check(Dropdown("Order").IsExpanded, "sort popup opens");
        yield return Escape();
        yield return new WaitForSecondsRealtime(.2f); // TMP's popup closes with an unscaled fade.
        Check(menu.Suspended && GameObject.Find("Debug panel") != null && !Dropdown("Order").IsExpanded, "back closes dropdown only");
        yield return Escape();
        Check(!menu.Suspended && menu.IsOpen && Time.timeScale == 0, "back returns to paused menu");
        menu.TryAccept(browser);
        yield return null;
        panel = GameObject.Find("Debug panel").transform;
        Check(Items().Length == total && Dropdown("Order").value == 1, "reopening remembers sort order");
        search = panel.GetComponentInChildren<TMP_InputField>();
        search.text = "rail";
        Dropdown("Type").value = 2;
        Dropdown("Rarity").value = 3;
        Dropdown("Collection").value = 1;
        yield return Click("SLOT 1");
        yield return Click("BACK");
        menu.TryAccept(browser);
        yield return null;
        panel = GameObject.Find("Debug panel").transform;
        Check(panel.GetComponentInChildren<TMP_InputField>().text == "rail" && Dropdown("Type").value == 2 &&
            Dropdown("Rarity").value == 3 && Dropdown("Collection").value == 1 && Dropdown("Order").value == 1,
            "search and combined filters persist within the run");
        yield return Click("RESET FILTERS");
        var catalogue = (RectTransform)panel.Find("Items/Catalogue");
        catalogue.anchoredPosition = new Vector2(0, 448);
        yield return Click("BACK");
        menu.TryAccept(browser);
        yield return null;
        panel = GameObject.Find("Debug panel").transform;
        Check(Mathf.Abs(((RectTransform)panel.Find("Items/Catalogue")).anchoredPosition.y - 448) < 1,
            "scroll position persists within the run");
        yield return Click("BACK");
        menu.setMenuOpen(false);
        Check(Time.timeScale == 1 && !Menu.GameplayPaused, "normal resume works after grants");
        Check(loadout.Weapons[1] is RailDriver && loadout.Actives[1] is MirrorBurst, "grants persist after resuming");
        GameRules.StartNewRun();
        menu.setMenuOpen(true);
        menu.TryAccept(browser);
        yield return null;
        panel = GameObject.Find("Debug panel").transform;
        Check(panel.GetComponentInChildren<TMP_InputField>().text == "" && Dropdown("Order").value == 0 &&
            ((RectTransform)panel.Find("Items/Catalogue")).anchoredPosition.y == 0, "new run resets browser state");
        yield return Click("BACK");
    }
}
#endif
