using UnityEngine;
using UnityEngine.UI;

/// <summary>Live industrial instruments shared by gameplay and the preview.</summary>
public sealed class IndustrialHUD : MonoBehaviour
{
    public Font font;
    public bool showcase;
    Shipcontroller player;
    Transform instrumentRoot;
    Text score, chain, coins;
    WaveOrchestrer waves;
    Image[] challengeCells;
    readonly RectTransform[] encounterRows = new RectTransform[System.Enum.GetValues(typeof(EncounterKind)).Length];
    int encounterRowsShown;
    // ELITE HUNT pips: waiting, caught (gold) or escaped (red), one per hunt wave.
    readonly Image[] huntPips = new Image[ChallengeProgress.Waves];
    // The district's seeded modifiers sit beneath the picked hexagons, in the same style.
    readonly RectTransform[] modifierRows = new RectTransform[LevelModifiers.All.Length];
    // The district's seeded condition or event (dusk, blizzard, rush hour...) leads the margin rows.
    RectTransform conditionRow;
    Image conditionIcon;
    Text conditionLabel;
    DistrictCondition conditionShown = DistrictCondition.Ordinary;
    Image cargoControl;
    RectTransform cargoCharge;
    Image[] healthCells, bossCells;
    RectTransform energyFill;
    Image energyLevel, energyImage;
    ShipLoadout loadout;
    readonly Image[] costLines = new Image[2];
    Image[] meterCasing;
    OctagonGraphic shieldGlow, shieldRing;
    static readonly Color ShieldBlue = new Color(.45f, .85f, 1f);
    RectTransform chainFill;
    RectTransform bossPanel;
    RectTransform scorePanel;
    RectTransform simulatorPanel;
    RectTransform sidePanel;
    CanvasGroup sideGroup;
    float entranceAge;
    public float EntranceAge => entranceAge;
    public bool EntranceComplete => entranceAge >= FluorescentStartup.CompleteAt && !shuttingDown;
    bool shuttingDown;
    public static void RestorePower()
    {
        var hud = FindAnyObjectByType<IndustrialHUD>();
        if (hud != null) hud.shuttingDown = false;
    }

    /// <summary>Switches every instrument off with the startup flicker played in reverse.</summary>
    public static void ShutDown()
    {
        var hud = FindAnyObjectByType<IndustrialHUD>();
        if (hud == null || hud.shuttingDown)
            return;
        hud.shuttingDown = true;
        // Just inside the flicker, so every tube keeps sampling the clock as it runs back to zero.
        hud.entranceAge = Mathf.Min(hud.entranceAge, FluorescentStartup.Duration - .001f);
        foreach (var tube in FindObjectsByType<FluorescentStartup>(FindObjectsInactive.Include))
            tube.enabled = true;
    }
    Boss boss;
    DistrictBoss districtBoss;
    DistrictMiniboss miniboss;
    Text minibossClock;
    Image[] phasePips;
    float nextScan, nextUpdate;
    float bottomLimit, topLimit;
    bool combatLimitsReady;
    static readonly Color Unlit = new Color(.1f, .135f, .14f);
    static readonly Color Steel = new Color(.73f, .77f, .72f), Amber = new Color(.83f, .62f, .30f), Cyan = new Color(.48f, .70f, .72f), Dark = new Color(.07f, .09f, .09f, .94f), Frame = new Color(.21f, .27f, .27f), Alarm = new Color(1, .16f, .12f);

    void Start()
    {
        // The legacy side-cover script accumulates offsets when the Game view resizes.
        foreach (var cover in FindObjectsByType<SideCoverController>())
            cover.enabled = false;
        var legacyUI = GameObject.Find("UI");
        if (legacyUI != null)
            foreach (var child in legacyUI.GetComponentsInChildren<Transform>(true))
                if (child.name == "CoverLeft" || child.name == "CoverRight")
                    child.gameObject.SetActive(false);
        var root = new GameObject("Industrial instruments", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        root.transform.SetParent(transform, false);
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        root.GetComponent<Canvas>().sortingOrder = 5;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 1;
        var left = Panel(root.transform, "Left playfield mask", Color.black);
        left.anchorMin = Vector2.zero;
        left.anchorMax = new Vector2(.5f, 1);
        left.offsetMin = Vector2.zero;
        left.offsetMax = new Vector2(-202.5f, 0);
        var right = Panel(root.transform, "Right playfield mask", Color.black);
        right.anchorMin = new Vector2(.5f, 0);
        right.anchorMax = Vector2.one;
        right.offsetMin = new Vector2(202.5f, 0);
        right.offsetMax = Vector2.zero;
        instrumentRoot = root.transform;
        if (!HangarHub.Active)
            PrepareInstruments();
    }

    public void PrepareInstruments()
    {
        if (scorePanel != null)
            return;
        gameObject.AddComponent<BossPositionShadow>();
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Shipcontroller>();
        waves = FindAnyObjectByType<WaveOrchestrer>();
        var frame = new GameObject("405 x 720 playfield", typeof(RectTransform));
        frame.transform.SetParent(instrumentRoot, false);
        var rect = frame.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(405, 720);
        scorePanel = Panel(frame.transform, "Score, multiplier and wallet", Dark);
        Place(scorePanel, -144, 12, 132, 84);
        Dress(scorePanel, .1f);
        FluorescentStartup.Attach(scorePanel.gameObject, .1f);
        Rule(scorePanel, 8, 3, 116, new Color(.21f, .27f, .27f));
        Rule(scorePanel, 8, 81, 116, new Color(.21f, .27f, .27f));
        score = TextAt(scorePanel, "000000000", 12, 10, 12, Steel);
        score.rectTransform.sizeDelta = new Vector2(108, 20);
        chain = TextAt(scorePanel, "x1", 12, 32, 12, Amber);
        chain.rectTransform.sizeDelta = new Vector2(108, 18);
        chain.alignment = TextAnchor.MiddleCenter;
        var chainTrack = Panel(scorePanel, "Multiplier progress line", new Color(.18f, .22f, .21f));
        Place(chainTrack, 30, 51, 72, 3);
        chainFill = Panel(chainTrack, "Multiplier progress", Amber);
        Place(chainFill, 0, 0, 0, 3);
        Place(Panel(scorePanel, "Coin rim", Amber), 13, 61, 12, 8);
        Place(Panel(scorePanel, "Coin shoulders", Amber), 15, 59, 8, 12);
        Place(Panel(scorePanel, "Coin stamp", Dark), 18, 62, 2, 6);
        coins = TextAt(scorePanel, "0", 32, 55, 12, Amber);
        coins.rectTransform.sizeDelta = new Vector2(48, 20);
        coins.alignment = TextAnchor.MiddleLeft;
        foreach (var text in new[] { score, chain, coins })
            text.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, .7f);
        challengeCells = Cells(scorePanel, 84, 64, 3, 9, new Color(.76f, .48f, .95f));
        sidePanel = Panel(frame.transform, "Side instruments", Dark);
        sidePanel.gameObject.AddComponent<RectMask2D>();
        sideGroup = FluorescentStartup.Attach(sidePanel.gameObject);
        sidePanel.anchorMin = sidePanel.anchorMax = Vector2.zero;
        sidePanel.pivot = Vector2.zero;
        sidePanel.sizeDelta = new Vector2(40, 248);
        sidePanel.anchoredPosition = new Vector2(-44, 6);
        Dress(sidePanel, .15f);
        meterCasing = new[]{Panel(sidePanel,"Left casing",Frame).GetComponent<Image>(),Panel(sidePanel,"Right casing",Frame).GetComponent<Image>(),
            Panel(sidePanel,"Top casing",Steel).GetComponent<Image>(),Panel(sidePanel,"Bottom casing",Frame).GetComponent<Image>()};
        Place(meterCasing[0].rectTransform, 1, 3, 2, 242);
        Place(meterCasing[1].rectTransform, 37, 3, 2, 242);
        Place(meterCasing[2].rectTransform, 3, 3, 34, 2);
        Place(meterCasing[3].rectTransform, 3, 244, 34, 2);
        Place(Panel(sidePanel, "Health cross", Steel), 7, 16, 11, 3);
        Place(Panel(sidePanel, "Health cross stem", Steel), 11, 12, 3, 11);
        Place(Panel(sidePanel, "Reactor indicator", Cyan), 25, 13, 7, 8);
        BuildHealthCells(Shipcontroller.StartingHits);
        BuildEnergyColumn();
        Fill(healthCells, 0, Steel);
        // The inventory occupies the first 40 pixels of the right margin.
        bossPanel = Panel(frame.transform, "Boss hull", Dark);
        Place(bossPanel, 455, 12, 28, 248);
        bossPanel.gameObject.AddComponent<RectMask2D>();
        Dress(bossPanel, .15f);
        FluorescentStartup.Attach(bossPanel.gameObject, .18f);
        Place(Panel(bossPanel, "Left casing", new Color(.21f, .27f, .27f)), 1, 3, 1, 242);
        Place(Panel(bossPanel, "Right casing", new Color(.21f, .27f, .27f)), 26, 3, 1, 242);
        Rule(bossPanel, 6, 8, 16, Amber);
        Rule(bossPanel, 6, 244, 16, new Color(.21f, .27f, .27f));
        bossCells = VerticalCells(bossPanel, 7, 17, 32, 14, 5, Amber);
        bossPanel.gameObject.SetActive(false);
        // One pip per boss life bar still to come after the current one.
        phasePips = new Image[5];
        for (int i = 0; i < phasePips.Length; i++)
        {
            var pip = Panel(bossPanel, "Phase pip " + i, Amber);
            Place(pip, 6 + i * 5, 262, 4, 5);
            phasePips[i] = pip.GetComponent<Image>();
        }
        minibossClock = TextAt(bossPanel, "", 2, 250, 12, Amber);
        minibossClock.rectTransform.sizeDelta = new Vector2(24, 20);
        minibossClock.alignment = TextAnchor.MiddleCenter;
        BuildSimulatorReadout(frame.transform);
    }

    void BuildSimulatorReadout(Transform frame)
    {
        var trial = FlightTrials.Active;
        if (trial == null) return;
        // Left margin, under the score panel and its encounter rows: the right margin belongs to
        // the collected items and the boss meter (Miguel, 14 November 2026).
        simulatorPanel = Panel(frame, "Simulator objective", Dark);
        Place(simulatorPanel, -184, 108, 172, 180);
        Dress(simulatorPanel, .1f);
        FluorescentStartup.Attach(simulatorPanel.gameObject, .22f);
        var accent = new Color(.70f, .65f, .98f);
        var heading = TextAt(simulatorPanel, "SIMULATOR", 10, 9, 14, accent);
        heading.rectTransform.sizeDelta = new Vector2(152, 20);
        float y = 35;
        // The tape's flight rule (underwater handling, guns offline...) is named under its objective.
        string rule = SimulatorFlight.Label(trial.approach);
        foreach (var line in string.IsNullOrEmpty(rule) ? new[] { trial.title, trial.objective } : new[] { trial.title, trial.objective, rule })
        {
            var label = TextAt(simulatorPanel, line, 10, y, 12, Steel);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.rectTransform.sizeDelta = new Vector2(152, 20);
            float height = Mathf.Max(20, label.preferredHeight);
            label.rectTransform.sizeDelta = new Vector2(152, height);
            y += height + 10;
        }
        simulatorPanel.sizeDelta = new Vector2(172, y + 3);
    }

    void LateUpdate()
    {
        // Compress into narrow side margins; never push an instrument into combat.
        float margin = Mathf.Max(0, (Screen.width * 720f / Mathf.Max(1, Screen.height) - 405) * .5f);
        if (scorePanel != null)
        {
            float scale = Mathf.Min(1, Mathf.Max(0, margin - 12) / 132);
            scorePanel.localScale = Vector3.one * scale;
            scorePanel.anchoredPosition = new Vector2(-12 - 132 * scale, -12);
        }
        if (bossPanel != null)
            bossPanel.localScale = Vector3.one * Mathf.Min(1, Mathf.Max(0, margin - 58) / 28);
        if (simulatorPanel != null)
        {
            float scoreScale = scorePanel != null ? scorePanel.localScale.x : 1;
            float rows = encounterRowsShown > 0 ? 88 + encounterRowsShown * 32 : 84;
            float scale = Mathf.Min(1, Mathf.Max(0, margin - 24) / 172);
            simulatorPanel.localScale = Vector3.one * scale;
            simulatorPanel.anchoredPosition = new Vector2(-12 - 172 * scale, -(12 + rows * scoreScale + 10));
            simulatorPanel.gameObject.SetActive(FlightTrials.Active != null && !HangarHub.Active && !GameOverState.Active);
        }
        if (sidePanel != null)
        {
            sidePanel.anchoredPosition = new Vector2((float)Screen.width / Screen.height > .72f ? -44 : 6, 6);
        }
        // Dock travel and arrival use the hangar's world-space limits.
        if (HangarHub.Active || player == null || !player.enabled)
            return;
        // Wait for the restored combat camera and pilot before measuring their playfield.
        if (!combatLimitsReady)
        {
            var camera = UnityEngine.Camera.main;
            if (camera == null) return;
            Physics2D.SyncTransforms();
            var hitbox = player.GetComponent<Collider2D>();
            float cockpitInset = hitbox != null ? player.transform.position.y - hitbox.bounds.min.y : 1.75f;
            bottomLimit = camera.transform.position.y - camera.orthographicSize + cockpitInset;
            topLimit = camera.transform.position.y + camera.orthographicSize - .65f;
            combatLimitsReady = true;
        }
        var position = player.transform.position;
        position.y = Mathf.Clamp(position.y, bottomLimit, topLimit);
        player.transform.position = position;
    }

    void Update()
    {
        if (player != null)
        {
            // The shared flicker clock waits at zero, all instruments dark, until the run leaves the hangar.
            if (shuttingDown)
                entranceAge = Mathf.Max(0, entranceAge - Time.unscaledDeltaTime);
            else if (!HangarHub.Active && !HangarHub.Launching)
                entranceAge += Time.unscaledDeltaTime;
            // Charge the instruments one segment at a time, without changing the player's resources.
            int hits = Mathf.Clamp(Mathf.RoundToInt(player.maxHealth), 1, MaxHitCells);
            if (hits != healthCells.Length)
                BuildHealthCells(hits);
            int healthLimit = Mathf.FloorToInt(Mathf.Clamp01((entranceAge - .75f) / .84f) * hits);
            float energyLimit = Mathf.Clamp01((entranceAge - 1.72f) / .84f);
            float health = player.health / Mathf.Max(1, player.maxHealth);
            Fill(healthCells, health, HealthRecovery.Tint(health <= .25f ? Color.Lerp(Steel, Alarm, .55f) : Steel), healthLimit);
            UpdateMeterWarning(player.health / Mathf.Max(1, player.maxHealth), Time.unscaledTime);
            UpdateEnergyShield(healthLimit, Time.unscaledTime);
            bool recharging = player.GetComponent<PassiveEnergyCore>() != null && !GameRules.playerDisabled && player.mana < player.maxMana;
            var energyColor = recharging ? Color.Lerp(Cyan, new Color(.55f, .95f, .82f), .3f + .3f * Mathf.Sin(Time.time * 4)) : Cyan;
            UpdateEnergyColumn(energyColor, energyLimit);
        }
        if (player == null || Time.unscaledTime < nextUpdate)
            return;
        nextUpdate = Time.unscaledTime + .1f;
        score.text = DimLeadingZeros(Score.CurrentScore.ToString("000000000"));
        chain.text = "x" + Score.CurrentMultiplier;
        coins.text = RunCoins.Count.ToString();
        // The orchestrator arrives with the additive Run scene, possibly after these instruments.
        if (waves == null)
            waves = FindAnyObjectByType<WaveOrchestrer>();
        UpdateEncounterRows();
        // The ELITE HUNT row's pips replace the old remaining-wave cells.
        for (int i = 0; i < challengeCells.Length; i++)
            challengeCells[i].gameObject.SetActive(false);
        chainFill.sizeDelta = new Vector2(72 * Mathf.Clamp01(Score.GetPercentageNextMultiplier()), 3);
        if (Time.unscaledTime > nextScan)
        {
            nextScan = Time.unscaledTime + .25f;
            boss = FindAnyObjectByType<Boss>();
            districtBoss = null;
            foreach (var candidate in FindObjectsByType<DistrictBoss>())
                if (candidate.UsesHealthBar)
                {
                    districtBoss = candidate;
                    break;
                }
            miniboss = FindAnyObjectByType<DistrictMiniboss>();
        }
        bool showMini = miniboss != null && !miniboss.Retreating && miniboss.health > 0;
        bossPanel.gameObject.SetActive(boss != null || districtBoss != null || showMini);
        bossPanel.sizeDelta = new Vector2(28, showMini || districtBoss != null ? 278 : 248);
        int remaining = !showMini && districtBoss != null ? districtBoss.PhaseCount - districtBoss.CurrentPhase : 0;
        for (int i = 0; i < phasePips.Length; i++)
            phasePips[i].gameObject.SetActive(i < remaining);
        minibossClock.text = showMini ? Mathf.CeilToInt(miniboss.Remaining).ToString() : "";
        minibossClock.color = showMini && miniboss.Remaining < 5 ? Alarm : Amber;
        if (showMini)
            Fill(bossCells, miniboss.HealthFraction, Amber);
        if (districtBoss != null)
        {
            // The emptied part of the current bar shows the next bar's colour waiting underneath.
            int phase = districtBoss.CurrentPhase;
            var below = phase < districtBoss.PhaseCount ? PhaseColor(phase + 1) * .45f : Unlit;
            below.a = 1;
            Fill(bossCells, districtBoss.PhaseFraction, PhaseColor(phase), int.MaxValue, below);
        }
        if (boss != null)
            Fill(bossCells, boss.HealthFraction, Amber);
    }

    // A continuous column: the fill rises from the bottom, and each equipped active marks its price with a line.
    const float EnergyX = 25, EnergyTop = 34, EnergyWidth = 7;
    void BuildEnergyColumn()
    {
        var track = Panel(sidePanel, "Energy column", Unlit);
        Place(track, EnergyX, EnergyTop, EnergyWidth, sidePanel.rect.height - EnergyTop - 12);
        energyFill = Panel(track, "Energy", Cyan);
        energyFill.anchorMin = Vector2.zero;
        energyFill.anchorMax = new Vector2(1, 0);
        energyFill.offsetMin = energyFill.offsetMax = Vector2.zero;
        energyImage = energyFill.GetComponent<Image>();
        // A brighter rim on the surface of the charge.
        var level = Panel(energyFill, "Energy level", Color.white);
        level.anchorMin = new Vector2(0, 1);
        level.anchorMax = Vector2.one;
        level.pivot = new Vector2(.5f, 1);
        level.offsetMin = new Vector2(0, -1);
        level.offsetMax = Vector2.zero;
        energyLevel = level.GetComponent<Image>();
        for (int i = 0; i < costLines.Length; i++)
        {
            // Each line overhangs the column on its own side, so equal prices still show both colours.
            var line = Panel(track, "Active " + (i + 1) + " cost", LoadoutHUD.ActiveColors[i]);
            line.anchorMin = line.anchorMax = Vector2.zero;
            line.pivot = new Vector2(0, .5f);
            line.sizeDelta = new Vector2(EnergyWidth + 4, 2);
            costLines[i] = line.GetComponent<Image>();
        }
    }

    void UpdateEnergyColumn(Color color, float limit)
    {
        float max = Mathf.Max(1, player.maxMana);
        float shown = Mathf.Min(limit, Mathf.Clamp01(player.mana / max));
        energyFill.anchorMax = new Vector2(1, shown);
        energyImage.color = color;
        energyLevel.color = shown > 0 ? Color.Lerp(color, Color.white, .45f) : Color.clear;
        if (loadout == null)
            loadout = player.GetComponent<ShipLoadout>();
        bool liveWire = player.GetComponent<CombatUpgrades>()?.Count(UpgradeKind.LiveWire) > 0;
        float height = ((RectTransform)energyFill.parent).rect.height;
        for (int i = 0; i < costLines.Length; i++)
        {
            var active = loadout != null ? loadout.Actives[i] : null;
            float cost = active != null && !liveWire ? active.EffectivePressCost : 0;
            var line = costLines[i];
            line.enabled = limit >= 1 && cost > 0;
            if (!line.enabled)
                continue;
            line.rectTransform.anchoredPosition = new Vector2(i == 0 ? -4 : 0, Mathf.Min(1, cost / max) * height);
            // Unaffordable prices sit dimmer above the charge.
            var tint = LoadoutHUD.ActiveColors[i];
            tint.a = player.mana >= cost ? 1 : .45f;
            line.color = tint;
        }
    }

    static Color PhaseColor(int phase) => phase switch
    {
        1 => Amber,
        2 => new Color(1, .45f, .18f),
        3 => Alarm,
        4 => new Color(.72f, .35f, 1),
        5 => new Color(.41f, .57f, 1),
        _ => new Color(.94f, .67f, 1)
    };

    static void Fill(Image[] cells, float value, Color color, int limit = int.MaxValue) => Fill(cells, value, color, limit, Unlit);

    static void Fill(Image[] cells, float value, Color color, int limit, Color empty)
    {
        int count = Mathf.Min(limit, Mathf.CeilToInt(Mathf.Clamp01(value) * cells.Length));
        for (int i = 0; i < cells.Length; i++)
        {
            // Lit segments brighten toward the top of the column; the topmost lit one glows.
            float light = Mathf.Lerp(.8f, 1.04f, cells.Length > 1 ? i / (cells.Length - 1f) : 1) + (i == count - 1 ? .12f : 0);
            cells[i].color = i < count ? new Color(color.r * light, color.g * light, color.b * light, color.a) : empty;
        }
    }

    static string DimLeadingZeros(string digits)
    {
        int first = 0;
        while (first < digits.Length - 1 && digits[first] == '0')
            first++;
        return first == 0 ? digits : "<color=#3C4A49>" + digits.Substring(0, first) + "</color>" + digits.Substring(first);
    }

    // A bevelled face behind the panel's own instruments; the panel image itself stays clear.
    void Dress(RectTransform panel, float cut)
    {
        panel.GetComponent<Image>().color = Color.clear;
        var face = Graphic<OctagonGraphic>(panel, "Plate face", Dark);
        face.cut = cut;
        face.shade = .45f;
        var rim = Graphic<OctagonGraphic>(panel, "Plate rim", new Color(.36f, .45f, .45f, .55f));
        rim.cut = cut;
        rim.thickness = 1;
        rim.shade = .7f;
        face.transform.SetSiblingIndex(0);
        rim.transform.SetSiblingIndex(1);
    }

    T Graphic<T>(Transform parent, string name, Color color) where T : MaskableGraphic
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(T));
        go.transform.SetParent(parent, false);
        var graphic = go.GetComponent<T>();
        graphic.color = color;
        graphic.raycastTarget = false;
        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return graphic;
    }

    void UpdateMeterWarning(float healthFraction, float time)
    {
        bool lit = EntranceComplete && !GameOverState.Active && healthFraction > 0 && healthFraction <= .25f && Mathf.Repeat(time, .8f) < .4f;
        for (int i = 0; i < meterCasing.Length; i++)
            meterCasing[i].color = lit ? Alarm : i == 2 ? Steel : Frame;
    }

    // One chunky cell per hit, bottom first; the column keeps its height as maximum hits grow.
    const int MaxHitCells = 12;
    void BuildHealthCells(int count)
    {
        if (healthCells != null)
            foreach (var cell in healthCells)
                Destroy(cell.gameObject);
        const float top = 34, gap = 4;
        float height = (sidePanel.rect.height - top - 12 - gap * (count - 1)) / count;
        healthCells = new Image[count];
        for (int i = 0; i < count; i++)
        {
            var cell = Panel(sidePanel, "Hit segment " + i, Steel);
            Place(cell, 8, top + (count - 1 - i) * (height + gap), 10, height);
            healthCells[i] = cell.GetComponent<Image>();
        }
        Fill(healthCells, 0, Steel);
        // The shield follows the next cell to be lost; keep its glow behind the health column.
        if (shieldGlow == null)
        {
            shieldGlow = ShieldPart("Energy shield glow", 0);
            shieldRing = ShieldPart("Energy shield ring", 1.5f);
        }
        shieldGlow.transform.SetSiblingIndex(healthCells[0].transform.GetSiblingIndex());
        shieldRing.transform.SetAsLastSibling();
    }

    OctagonGraphic ShieldPart(string name, float thickness)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(OctagonGraphic));
        go.transform.SetParent(sidePanel, false);
        var graphic = go.GetComponent<OctagonGraphic>();
        graphic.raycastTarget = false;
        graphic.cut = .3f;
        graphic.thickness = thickness;
        graphic.enabled = false;
        return graphic;
    }

    void UpdateEnergyShield(int healthLimit, float time)
    {
        var cell = player.GetComponent<EnergyShieldCell>();
        int count = Mathf.Min(healthLimit, Mathf.CeilToInt(Mathf.Clamp01(player.health / Mathf.Max(1, player.maxHealth)) * healthCells.Length));
        shieldGlow.enabled = shieldRing.enabled = count > 0 && cell != null;
        if (!shieldRing.enabled)
            return;
        var first = healthCells[count - 1].rectTransform;
        Place(shieldGlow.rectTransform, first.anchoredPosition.x - 3, -first.anchoredPosition.y - 3, first.sizeDelta.x + 6, first.sizeDelta.y + 6);
        Place(shieldRing.rectTransform, first.anchoredPosition.x - 3, -first.anchoredPosition.y - 3, first.sizeDelta.x + 6, first.sizeDelta.y + 6);
        // Ready: a steady breathing glow. Recharging: a faint ring that strengthens toward full.
        float pulse = .8f + .2f * Mathf.Sin(time * 3);
        var flash = Color.Lerp(ShieldBlue, Color.white, cell.Flash);
        shieldRing.color = new Color(flash.r, flash.g, flash.b, cell.Ready ? pulse : .12f + .33f * cell.Charge + cell.Flash);
        shieldGlow.color = new Color(flash.r, flash.g, flash.b, cell.Ready ? .45f * pulse : cell.Flash * .8f);
    }

    Image[] Cells(Transform parent, float x, float y, int count, float width, Color color)
    {
        var result = new Image[count];
        for (int i = 0; i < count; i++)
        {
            var cell = Panel(parent, "Segment " + i, color);
            Place(cell, x + i * (width + 2), y, width, 5);
            result[i] = cell.GetComponent<Image>();
        }
        return result;
    }

    Image[] VerticalCells(Transform parent, float x, float y, int count, float width, float height, Color color)
    {
        float available = ((RectTransform)parent).rect.height - y - 12;
        height = Mathf.Min(height, available / count);
        float gap = count > 1 ? Mathf.Min(2, Mathf.Max(0, (available - count * height) / (count - 1))) : 0;
        var result = new Image[count];
        for (int i = 0; i < count; i++)
        {
            var cell = Panel(parent, "Vertical segment " + i, color);
            Place(cell, x, y + (count - 1 - i) * (height + gap), width, height);
            result[i] = cell.GetComponent<Image>();
        }
        return result;
    }

    void Rule(Transform parent, float x, float y, float width, Color color)
    {
        Place(Panel(parent, "Panel seam", color), x, y, width, 1);
    }

    void UpdateEncounterRows()
    {
        int visible = 0;
        for (int i = 0; i < encounterRows.Length; i++)
        {
            var kind = (EncounterKind)i;
            var row = encounterRows[i];
            if (waves == null || !waves.ShowingEncounter(kind) || GameOverState.Active)
            {
                if (row != null && row.gameObject.activeSelf) row.gameObject.SetActive(false);
                continue;
            }
            if (row == null)
            {
                string caption = EncounterChoice.Label(kind);
                encounterRows[i] = row = Panel(scorePanel, caption, Dark);
                var icon = Panel(row, "Selected hexagon", Color.white);
                Place(icon, 3, 2, 26, 26);
                icon.GetComponent<Image>().sprite = EncounterChoice.Icon(kind);
                icon.GetComponent<Image>().preserveAspect = true;
                var label = TextAt(row, caption, 33, 3, 12, Steel);
                label.rectTransform.sizeDelta = new Vector2(kind == EncounterKind.Cargo ? 65 : kind == EncounterKind.Challenge ? 58 : 96, 24);
                label.alignment = TextAnchor.MiddleLeft;
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 8;
                label.resizeTextMaxSize = 12;
                if (kind == EncounterKind.Challenge)
                    for (int pip = 0; pip < huntPips.Length; pip++)
                    {
                        var cell = Panel(row, "Hunt pip " + pip, Steel);
                        Place(cell, 94 + pip * 12, 10, 9, 10);
                        huntPips[pip] = cell.GetComponent<Image>();
                    }
                if (kind == EncounterKind.Cargo)
                {
                    var control = Panel(row, "Open cargo control", Color.white);
                    Place(control, 101, 1, 30, 27);
                    cargoControl = control.GetComponent<Image>();
                    cargoControl.preserveAspect = true;
                    cargoCharge = Panel(row, "Cargo opening hold", Color.white);
                    Place(cargoCharge, 105, 27, 0, 2);
                }
            }
            if (kind == EncounterKind.Challenge && huntPips[0] != null)
                for (int pip = 0; pip < huntPips.Length; pip++)
                {
                    int state = waves.HuntPip(pip);
                    huntPips[pip].color = state == 1 ? new Color(1, .82f, .3f) : state == 2 ? Alarm : new Color(.22f, .27f, .27f);
                }
            if (kind == EncounterKind.Cargo && cargoControl != null)
            {
                var action = waves.Cargo.OpenControl;
                cargoControl.enabled = action != null && GameClock.ActivePlay;
                cargoControl.sprite = InputPrompts.For(action);
                cargoCharge.sizeDelta = new Vector2(22 * Mathf.Clamp01(waves.Cargo.ClaimProgress), 2);
            }
            Place(row, 0, 88 + visible++ * 32, 132, 30);
            if (!row.gameObject.activeSelf) row.gameObject.SetActive(true);
        }
        var condition = waves != null && !GameOverState.Active && !HangarHub.Active ? DistrictConditions.Current : DistrictCondition.Ordinary;
        if (condition == DistrictCondition.Ordinary)
        {
            if (conditionRow != null && conditionRow.gameObject.activeSelf) conditionRow.gameObject.SetActive(false);
        }
        else
        {
            if (conditionRow == null)
            {
                conditionRow = Panel(scorePanel, "District condition", Dark);
                var icon = Panel(conditionRow, "District condition icon", Color.white);
                Place(icon, 3, 2, 26, 26);
                conditionIcon = icon.GetComponent<Image>();
                conditionIcon.preserveAspect = true;
                conditionLabel = TextAt(conditionRow, "", 33, 3, 12, Steel);
                conditionLabel.rectTransform.sizeDelta = new Vector2(96, 24);
                conditionLabel.alignment = TextAnchor.MiddleLeft;
                conditionLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                conditionLabel.resizeTextForBestFit = true;
                conditionLabel.resizeTextMinSize = 8;
                conditionLabel.resizeTextMaxSize = 12;
            }
            if (conditionShown != condition)
            {
                conditionShown = condition;
                conditionIcon.sprite = DistrictConditions.Icon(condition);
                conditionLabel.text = DistrictConditions.Label(condition);
            }
            Place(conditionRow, 0, 88 + visible++ * 32, 132, 30);
            if (!conditionRow.gameObject.activeSelf) conditionRow.gameObject.SetActive(true);
        }
        var modifiers = waves != null && !GameOverState.Active && !HangarHub.Active ? LevelModifiers.Current : LevelModifier.None;
        for (int i = 0; i < modifierRows.Length; i++)
        {
            var modifier = LevelModifiers.All[i];
            var row = modifierRows[i];
            if ((modifiers & modifier) == 0)
            {
                if (row != null && row.gameObject.activeSelf) row.gameObject.SetActive(false);
                continue;
            }
            if (row == null)
            {
                string caption = LevelModifiers.Label(modifier);
                modifierRows[i] = row = Panel(scorePanel, caption, Dark);
                var icon = Panel(row, "District modifier", Color.white);
                Place(icon, 3, 2, 26, 26);
                icon.GetComponent<Image>().sprite = LevelModifiers.Icon(modifier);
                icon.GetComponent<Image>().preserveAspect = true;
                var label = TextAt(row, caption, 33, 3, 12, Steel);
                label.rectTransform.sizeDelta = new Vector2(96, 24);
                label.alignment = TextAnchor.MiddleLeft;
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 8;
                label.resizeTextMaxSize = 12;
            }
            Place(row, 0, 88 + visible++ * 32, 132, 30);
            if (!row.gameObject.activeSelf) row.gameObject.SetActive(true);
        }
        encounterRowsShown = visible;
    }

    RectTransform Panel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return go.GetComponent<RectTransform>();
    }

    Text TextAt(Transform parent, string value, float x, float y, int size, Color color)
    {
        var go = new GameObject(value, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.text = value;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        Place(go.GetComponent<RectTransform>(), x, y, 377, 20);
        return text;
    }

    static void Place(RectTransform rect, float x, float y, float w, float h)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(w, h);
    }
}
