using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// A build-accessible pattern viewer. Enemies, movement, aim and projectiles are the real combat code.
public sealed partial class WaveWatcher : MonoBehaviour
{
    public const int FirstWave = 2, LastWave = 20, Miniboss = 21, BossOne = 22, BossTwo = 23, BossOther = 24;
    const string DifficultyPreference = "WaveWatcher.Difficulty";
    static bool requested;
    public static WaveWatcher Current
    {
        get; private set;
    }

    public LevelId Level => LevelRun.Current;
    public int Encounter { get; private set; } = FirstWave;
    public bool Paused
    {
        get; private set;
    }

    public bool Looping { get; private set; } = true;
    public float PlaybackSpeed { get; private set; } = 1;
    public float Elapsed
    {
        get; private set;
    }

    public bool Ready
    {
        get; private set;
    }

    public int Spawned
    {
        get; private set;
    }

    public WatcherView View
    {
        get; private set;
    }

    public int Selection
    {
        get; private set;
    }

    public int Variant
    {
        get; private set;
    }

    public int ResolvedVariant
    {
        get; private set;
    }

    public int Mirror { get; private set; } = 1;
    public int ReviewLoop { get; private set; } = 1;
    public RunDifficulty ReviewDifficulty => Difficulty.Current;
    public bool Touring
    {
        get; private set;
    }

    public int AimSide => aim;
    public IReadOnlyList<WatcherCatalog.Entry> Entries => catalog;
    public WatcherCatalog.Entry SelectedEntry => catalog[Selection];
    List<WatcherCatalog.Entry> catalog;
    string recipe = "";
    int pendingSteps;
    bool stepScheduled;
    float savedCaptureDelta;
    Shipcontroller ship;
    Coroutine playback;
    Canvas[] hiddenCanvases;
    TMP_FontAsset font;
    TextMeshProUGUI heading, status, pauseLabel, loopLabel, speedLabel, partLabel;
    readonly Dictionary<LevelId, Image> levels = new Dictionary<LevelId, Image>();
    readonly Dictionary<int, Image> encounters = new Dictionary<int, Image>();
    readonly List<Image> aims = new List<Image>();
    int aim;
    float nextStatus;
    static readonly Color Panel = new Color(.035f, .055f, .06f, 1), ButtonColor = new Color(.10f, .15f, .16f, 1), Selected = new Color(.52f, .39f, .18f, 1);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        requested = false;
        Current = null;
        SceneManager.sceneLoaded -= SceneReady;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        SceneManager.sceneLoaded += SceneReady;
    }

    static void SceneReady(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Title")
        {
            requested = false;
            Current = null;
            var start = FindAnyObjectByType<NewGame>();
            if (start == null)
                return;
            var menu = start.GetComponentInParent<Menu>();
            var row = new GameObject("Wave Watcher", typeof(RectTransform), typeof(Image));
            row.transform.SetParent(menu.transform, false);
            var rect = row.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(726, 52);
            rect.anchoredPosition = new Vector2(0, -190);
            row.AddComponent<WaveWatcherMenu>();
            var label = new GameObject("Wave Watcher label", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(row.transform, false);
            label.GetComponent<RectTransform>().sizeDelta = rect.sizeDelta;
            var text = label.GetComponent<TextMeshProUGUI>();
            text.font = menu.GetComponentInChildren<TextMeshProUGUI>().font;
            text.text = "WAVE WATCHER";
            text.fontSize = 24;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            menu.RefreshElements();
        }
        else if (scene.name == "L1" && requested)
        {
            var flow = FindAnyObjectByType<WaveOrchestrer>();
            if (flow != null)
            {
                flow.enabled = false;
                flow.CancelInvoke();
                flow.StopAllCoroutines();
            }
            Current = new GameObject("Wave watcher controls", typeof(RectTransform)).AddComponent<WaveWatcher>();
        }
    }

    public static void Launch()
    {
        if (CloudTransition.Busy || requested)
            return;
        requested = true;
        GameRules.StartNewRun((RunDifficulty)Mathf.Clamp(PlayerPrefs.GetInt(DifficultyPreference, (int)RunDifficulty.Hard), 0, 1));
        LevelRun.Reset();
        BGMManager.PlayLevelMusic(LevelRun.Current);
        CloudTransition.Load("L1");
    }

    IEnumerator Start()
    {
        yield return null; // Let ship art, instruments, weapons and input finish their normal startup.
        ship = FindAnyObjectByType<Shipcontroller>();
        if (ship == null)
        {
            Debug.LogError("Wave Watcher requires the gameplay ship");
            yield break;
        }
        ship.ObserverMode = true;
        ship.enabled = false;
        var body = ship.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.bodyType = RigidbodyType2D.Kinematic;
        }
        foreach (var weapon in ship.GetComponentsInChildren<AWeapon>())
        {
            weapon.StopFiring();
            weapon.enabled = false;
        }
        foreach (var menu in FindObjectsByType<Menu>())
        {
            menu.Suspended = true;
            menu.gameObject.SetActive(false);
        }
        var dialog = FindAnyObjectByType<Dialog>();
        if (dialog != null)
        {
            font = dialog.font;
            dialog.Hide();
        }
        var hidden = new List<Canvas>();
        foreach (var canvas in FindObjectsByType<Canvas>())
            if (canvas.name != "PSX pixel display" && canvas.name != "Cloud scene transitions")
                hidden.Add(canvas);
        hiddenCanvases = hidden.ToArray();
        catalog = WatcherCatalog.For(View, Level);
        Build();
        SetAim(0);
        HidePilot();
        while (CloudTransition.Busy || !WavePreparation.Ready)
            yield return null;
        yield return new WaitForSeconds(WaveOrchestrer.OpeningBreather);
        Ready = true;
        Replay();
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(levels[Level].gameObject);
    }

    void Update()
    {
        if (Ready && ((Keyboard.current?.escapeKey.wasPressedThisFrame ?? false) || (Gamepad.current?.buttonEast.wasPressedThisFrame ?? false)))
            Exit();
        if (!Ready || (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null))
            return;
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;
        if (keyboard.spaceKey.wasPressedThisFrame)
            TogglePause();
        if (keyboard.periodKey.wasPressedThisFrame)
            StepOnce();
        if (keyboard.rKey.wasPressedThisFrame)
            Replay();
        if (keyboard.cKey.wasPressedThisFrame)
            CopyForChat();
    }

    void LateUpdate()
    {
        if (ship == null)
            return;
        HidePilot();
        if (hiddenCanvases != null)
            foreach (var canvas in hiddenCanvases)
                if (canvas != null)
                    canvas.enabled = false;
        if (stepScheduled)
        {
            Time.captureDeltaTime = savedCaptureDelta;
            stepScheduled = false;
        }
        if (pendingSteps > 0 && Ready && !CloudTransition.Busy)
        {
            pendingSteps--;
            savedCaptureDelta = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60;
            Time.timeScale = 1;
            stepScheduled = true;
        }
        else
            Time.timeScale = CloudTransition.Busy ? 1 : Paused ? 0 : PlaybackSpeed;
        LayoutPanels();
        if (Time.unscaledTime < nextStatus)
            return;
        nextStatus = Time.unscaledTime + .15f;
        status.text = WavePreparation.Ready ? $"{Elapsed:0.00}s  /  {AEnemy.Live.Count} ships  /  {CombatRound.Live.Count} shots" : "PREPARING";
        if (Ready)
            SaveCurrentReference();
    }

    void HidePilot()
    {
        foreach (var renderer in ship.GetComponentsInChildren<Renderer>())
            renderer.forceRenderingOff = true;
        foreach (var collider in ship.GetComponentsInChildren<Collider2D>())
            collider.enabled = false;
    }

    public void SelectLevel(LevelId level)
    {
        if (!Ready || CloudTransition.Busy)
            return;
        SetContext(level, LevelRun.Seed);
        LevelEnvironment.Apply();
        BGMManager.PlayLevelMusic(level);
        catalog = WatcherCatalog.For(View, level);
        Selection = Mathf.Min(Selection, catalog.Count - 1);
        RebuildCatalog();
        SetAim(aim);
        Replay();
    }

    public void SelectEncounter(int encounter)
    {
        if (!Ready || encounter < FirstWave || encounter > BossOther)
            return;
        var view = encounter <= LastWave ? WatcherView.Waves : WatcherView.Bosses;
        var entries = WatcherCatalog.For(view, Level);
        int selected = entries.FindIndex(x => x.Encounter == encounter);
        if (selected < 0)
            return;
        View = view;
        catalog = entries;
        Selection = selected;
        RebuildCatalog();
        Replay();
    }

    public void Replay()
    {
        if (!Ready)
            return;
        if (playback != null)
            StopCoroutine(playback);
        Encounter = SelectedEntry.Encounter;
        playback = StartCoroutine(Play());
        Refresh();
    }

    public void TogglePause()
    {
        Paused = !Paused;
        Refresh();
    }

    public void ToggleLoop()
    {
        Looping = !Looping;
        Refresh();
    }

    public void CycleSpeed()
    {
        PlaybackSpeed = PlaybackSpeed == 1 ? .5f : PlaybackSpeed == .5f ? 2 : 1;
        Refresh();
    }

    public void NewSet()
    {
        if (Ready)
            SetSeed(UnityEngine.Random.Range(1, 10000000));
    }

    public void SetSeed(int seed)
    {
        if (!Ready)
            return;
        SetContext(Level, Mathf.Max(1, seed));
        LevelEnvironment.Apply();
        Replay();
    }

    void SetContext(LevelId level, int seed)
    {
        LevelRun.Reset(LevelId.Fields, seed);
        for (int i = 1; i < ReviewLoop; i++)
        {
            LevelRun.Advance(LevelId.Snow);
            LevelRun.Advance(LevelId.Thunder);
            LevelRun.Advance(LevelId.Space);
            LevelRun.Advance(LevelId.Hell);
            LevelRun.Advance(LevelId.Fields);
        }
        if (LevelRun.Tier(level) >= 2)
            LevelRun.Advance(LevelRun.Tier(level) == 2 ? level : level == LevelId.Tunnel || level == LevelId.Candyland ? LevelId.City : LevelId.Snow);
        if (LevelRun.Tier(level) >= 3)
            LevelRun.Advance(LevelRun.Tier(level) == 3 ? level : LevelId.Thunder);
        if (LevelRun.Tier(level) >= 4)
            LevelRun.Advance(LevelRun.Tier(level) == 4 ? level : LevelId.Space);
        if (LevelRun.Tier(level) == 5)
            LevelRun.Advance(LevelId.Hell);
    }

    public void SelectLoop(int loop)
    {
        if (!Ready)
            return;
        ReviewLoop = Mathf.Clamp(loop, 1, 99);
        SetContext(Level, LevelRun.Seed);
        WavePreparation.Begin();
        Replay();
    }

    public void SelectDifficulty(RunDifficulty difficulty)
    {
        if (!Ready || CloudTransition.Busy || difficulty == ReviewDifficulty ||
            (difficulty != RunDifficulty.Easy && difficulty != RunDifficulty.Hard))
            return;
        Difficulty.Select(difficulty);
        GameRules.enemyDamage = Difficulty.Damage;
        PlayerPrefs.SetInt(DifficultyPreference, (int)difficulty);
        PlayerPrefs.Save();
        WavePreparation.Begin();
        Replay();
    }

    public void SelectView(WatcherView view)
    {
        if (!Ready)
            return;
        View = view;
        Selection = 0;
        catalog = WatcherCatalog.For(view, Level);
        RebuildCatalog();
        Replay();
    }

    public void SelectEntry(int index)
    {
        if (!Ready || index < 0 || index >= catalog.Count)
            return;
        Selection = index;
        RebuildCatalog();
        Replay();
    }

    public void SelectVariant(int variant)
    {
        Variant = (variant % 13 + 13) % 13;
        Replay();
    }

    public void ToggleMirror()
    {
        Mirror = -Mirror;
        Replay();
    }

    public void StepOnce()
    {
        if (!Ready)
            return;
        Paused = true;
        pendingSteps++;
        Refresh();
    }

    public void ToggleTour()
    {
        Touring = !Touring;
        if (Touring)
        {
            Paused = false;
            Replay();
        }
        Refresh();
    }

    public void NextEntry(int direction)
    {
        if (Touring && direction > 0 && Selection == catalog.Count - 1)
        {
            if (View == WatcherView.Projectiles)
            {
                View = WatcherView.Waves;
                var order = (LevelId[])System.Enum.GetValues(typeof(LevelId));
                SelectLevel(order[(System.Array.IndexOf(order, Level) + 1) % order.Length]);
            }
            else
                View = (WatcherView)((int)View + 1);
            catalog = WatcherCatalog.For(View, Level);
            Selection = 0;
            RebuildCatalog();
            Replay();
            return;
        }
        SelectEntry((Selection + direction + catalog.Count) % catalog.Count);
    }

    public void SetAim(int side)
    {
        aim = Mathf.Clamp(side, -1, 1);
        if (ship != null)
            ship.transform.position = new Vector3(aim * Camera.RightLimit * .58f, Camera.DownLimit + 1.8f, -1);
        for (int i = 0; i < aims.Count; i++)
            aims[i].color = i - 1 == aim ? Selected : ButtonColor;
    }

    public void Exit()
    {
        if (CloudTransition.Busy)
            return;
        requested = false;
        Time.timeScale = 1;
        Paused = false;
        PlaybackSpeed = 1;
        CloudTransition.Load("Title");
    }

    void OnDestroy()
    {
        if (catalog != null)
            SaveCurrentReference(false);
        if (Current == this)
            Current = null;
        if (stepScheduled)
            Time.captureDeltaTime = savedCaptureDelta;
        Time.timeScale = 1;
    }

    static void ClearArena()
    {
        foreach (var enemy in FindObjectsByType<AEnemy>())
        {
            enemy.enabled = false;
            Destroy(enemy.gameObject);
        }
        foreach (var bullet in FindObjectsByType<AEnemyBullet>())
            bullet.Vanish();
        foreach (var bullet in FindObjectsByType<ABullet>())
            Destroy(bullet.gameObject);
        foreach (var pickup in FindObjectsByType<APickable>())
            Destroy(pickup.gameObject);
    }

    IEnumerator Play()
    {
        ClearArena();
        Elapsed = 0;
        Spawned = 0;
        recipe = "";
        ResolvedVariant = Variant;
        partLabel.text = "";
        yield return null;
        while (CloudTransition.Busy || !WavePreparation.Ready)
            yield return null;
        // Replay attack randomness as well as the authored composition.
        UnityEngine.Random.InitState(LevelRun.Seed);
        var entry = SelectedEntry;
        if (View == WatcherView.Projectiles)
        {
            var emitter = DistrictEnemy.Spawn(typeof(BeamDrone), new Vector3(0, Camera.UpLimit - 2.4f, 1));
            emitter.enabled = false;
            foreach (var mover in emitter.GetComponents<PatternMover>())
                mover.enabled = false;
            Spawned = 1;
            Refresh();
            float nextShot = 0;
            while (true)
            {
                if (Elapsed >= nextShot && (Looping || Touring || nextShot == 0))
                {
                    entry.Fire(emitter.transform.position, emitter.Hull);
                    nextShot = Elapsed + 4;
                }
                Elapsed += Time.deltaTime;
                if (Touring && Elapsed >= 16)
                {
                    NextEntry(1);
                    yield break;
                }
                yield return null;
            }
        }
        if (View == WatcherView.Bosses && Encounter >= BossOne)
        {
            var boss = (DistrictBoss)DistrictEnemy.Spawn(DistrictWaves.BossType, new Vector3(0, Camera.UpLimit + 3, 1));
            Spawned = 1;
            yield return null;
            yield return null; // Both City hulls finish Start before selecting the survivor phase.
            if (Encounter == BossTwo || Encounter == BossOther)
            {
                if (boss is CityDuoBoss duo && duo.partner != null)
                {
                    var victim = Encounter == BossOther ? duo.partner : duo;
                    var survivor = Encounter == BossOther ? duo : duo.partner;
                    survivor.health *= .55f;
                    victim.damage(1000000);
                }
                else if (boss is DesertWormBoss worm)
                {
                    for (int i = 1; i <= 4; i++)
                        worm.Cores[i].damage(1000000);
                }
                else if (boss is SpaceCitadelBoss station)
                {
                    station.Parts[0].damage(1000000);
                    station.Parts[1].damage(1000000);
                }
                else if (boss is ThunderLanceBoss thunder)
                    boss.health *= thunder.phaseTwoThreshold * .95f;
                else if (boss is HellCrabBoss crab)
                    crab.Eyes[0].damage(1000000);
                else
                    boss.health *= .49f;
            }
            recipe = WatcherCatalog.Pretty(DistrictWaves.BossType.Name);
            Refresh();
            while (true)
            {
                Elapsed += Time.deltaTime;
                if (Elapsed >= 24 && (Touring || Looping))
                {
                    if (Touring)
                        NextEntry(1);
                    else
                        Replay();
                    yield break;
                }
                yield return null;
            }
        }
        if (View == WatcherView.Enemies)
        {
            DistrictEnemy.Spawn(entry.Enemy, new Vector3(0, Camera.UpLimit + 1, 1));
            Spawned = 1;
            Refresh();
            float quietEnemy = 0;
            while (true)
            {
                Elapsed += Time.deltaTime;
                quietEnemy = AEnemy.Live.Count == 0 && CombatRound.Live.Count == 0 ? quietEnemy + Time.deltaTime : 0;
                if (quietEnemy > .8f && (Touring || Looping))
                {
                    if (Touring)
                        NextEntry(1);
                    else
                        Replay();
                    yield break;
                }
                yield return null;
            }
        }
        int resolved = Variant;
        var wave = View == WatcherView.Formations ? WaveComposer.PreviewPart(entry.Part, Variant, Mirror, out resolved) :
            Encounter == Miniboss ? DistrictWaves.Miniboss() : DistrictWaves.Create(Encounter);
        if (View == WatcherView.Formations)
            ResolvedVariant = resolved;
        if (wave == null)
        {
            recipe = "No safe layout for this variant";
            Refresh();
            if (Touring)
            {
                yield return new WaitForSeconds(1);
                NextEntry(1);
            }
            yield break;
        }
        recipe = View == WatcherView.Formations ? WatcherCatalog.Pretty(entry.Part.Enemy) + "\n" + entry.Part.Route.Name :
            Encounter == Miniboss ? WatcherCatalog.Pretty(wave.units[0].enemy) : string.Join("\n", wave.parts);
        Refresh();
        int next = 0;
        float quiet = 0;
        while (true)
        {
            Elapsed += Time.deltaTime;
            while (next < wave.units.Count && wave.units[next].time <= Elapsed)
            {
                var unit = wave.units[next++];
                var enemy = DistrictEnemy.Spawn(DistrictWaves.EnemyType(unit.enemy), new Vector3(unit.position.x * Camera.RightLimit, unit.position.y * Camera.UpLimit, 1));
                enemy.FlyPhrase(unit);
                Spawned++;
            }
            bool clear = next == wave.units.Count && AEnemy.Live.Count == 0 && CombatRound.Live.Count == 0;
            quiet = clear ? quiet + Time.deltaTime : 0;
            if ((Looping || Touring) && quiet > .8f)
            {
                if (Touring)
                    NextEntry(1);
                else
                    Replay();
                yield break;
            }
            yield return null;
        }
    }

    RectTransform Box(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        go.GetComponent<Image>().color = color;
        return rect;
    }

    TextMeshProUGUI Label(Transform parent, string value, float size, Vector2 position, Vector2 dimensions)
    {
        var go = new GameObject("Watcher label", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        var text = go.GetComponent<TextMeshProUGUI>();
        if (font != null)
            text.font = font;
        text.text = value;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.enableAutoSizing = true;
        text.fontSizeMin = 10;
        text.fontSizeMax = size;
        text.color = new Color(.83f, .87f, .81f);
        text.raycastTarget = false;
        return text;
    }

    Button Button(Transform parent, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction clicked)
    {
        var rect = Box(parent, label, position, size, ButtonColor);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        var colors = button.colors;
        colors.highlightedColor = new Color(1.25f, 1.25f, 1.1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        Label(rect, label, 16, Vector2.zero, size);
        button.onClick.AddListener(clicked);
        return button;
    }
}

public sealed class WaveWatcherMenu : AMenuElement
{
    public override void OnAccept() => WaveWatcher.Launch();
}
