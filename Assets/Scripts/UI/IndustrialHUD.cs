using UnityEngine;
using UnityEngine.UI;

/// <summary>Live industrial instruments shared by gameplay and the preview.</summary>
public sealed class IndustrialHUD : MonoBehaviour
{
    public Font font;
    public bool showcase;
    Shipcontroller player;
    Text score, chain, coins;
    WaveOrchestrer waves;
    Image[] challengeCells;
    Image[] healthCells, manaCells, bossCells;
    Image[] meterCasing;
    OctagonGraphic shieldGlow, shieldRing;
    static readonly Color ShieldBlue = new Color(.45f, .85f, 1f);
    RectTransform chainFill;
    RectTransform bossPanel;
    RectTransform scorePanel;
    RectTransform sidePanel;
    CanvasGroup sideGroup;
    float entranceAge;
    public float EntranceAge => entranceAge;
    public bool EntranceComplete => entranceAge >= FluorescentStartup.CompleteAt && !shuttingDown;
    bool shuttingDown;

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
    static readonly Color Unlit = new Color(.1f, .135f, .14f);
    static readonly Color Steel = new Color(.73f, .77f, .72f), Amber = new Color(.83f, .62f, .30f), Cyan = new Color(.48f, .70f, .72f), Dark = new Color(.07f, .09f, .09f, .94f), Frame = new Color(.21f, .27f, .27f), Alarm = new Color(1, .16f, .12f);

    void Start()
    {
        // Built while the hangar idles but kept dark: the startup flicker only begins with the run (see Update).
        gameObject.AddComponent<BossPositionShadow>();
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Shipcontroller>();
        waves = FindAnyObjectByType<WaveOrchestrer>();
        // The legacy side-cover script accumulates offsets when the Game view resizes.
        foreach (var cover in FindObjectsByType<SideCoverController>())
            cover.enabled = false;
        var legacyUI = GameObject.Find("UI");
        if (legacyUI != null)
            foreach (var child in legacyUI.GetComponentsInChildren<Transform>(true))
                if (child.name == "CoverLeft" || child.name == "CoverRight")
                    child.gameObject.SetActive(false);
        var camera = UnityEngine.Camera.main;
        float viewBottom = camera.transform.position.y - camera.orthographicSize;
        float viewHeight = camera.orthographicSize * 2;
        // Let the cockpit (the hitbox) come right down to the bottom edge: its lowest point may
        // touch the screen border while the rest of the hull dips off screen.
        var hitbox = player.GetComponent<Collider2D>();
        bottomLimit = hitbox != null ? viewBottom + (player.transform.position.y - hitbox.bounds.min.y) : viewBottom + 1.75f;
        topLimit = viewBottom + viewHeight - .65f;
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
        var frame = new GameObject("405 x 720 playfield", typeof(RectTransform));
        frame.transform.SetParent(root.transform, false);
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
        manaCells = VerticalCells(sidePanel, 25, 34, 24, 7, 7, Cyan);
        Fill(healthCells, 0, Steel);
        Fill(manaCells, 0, Cyan);
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
        phasePips = new Image[2];
        for (int i = 0; i < phasePips.Length; i++)
        {
            var pip = Panel(bossPanel, "Phase pip " + i, Amber);
            Place(pip, 8 + i * 7, 262, 5, 5);
            phasePips[i] = pip.GetComponent<Image>();
        }
        minibossClock = TextAt(bossPanel, "", 2, 250, 12, Amber);
        minibossClock.rectTransform.sizeDelta = new Vector2(24, 20);
        minibossClock.alignment = TextAnchor.MiddleCenter;
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
        if (sidePanel != null)
        {
            sidePanel.anchoredPosition = new Vector2((float)Screen.width / Screen.height > .72f ? -44 : 6, 6);
        }
        if (player == null || !player.enabled)
            return;
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
            int energyLimit = Mathf.FloorToInt(Mathf.Clamp01((entranceAge - 1.72f) / .84f) * 24);
            float health = player.health / Mathf.Max(1, player.maxHealth);
            Fill(healthCells, health, HealthRecovery.Tint(health <= .25f ? Color.Lerp(Steel, Alarm, .55f) : Steel), healthLimit);
            UpdateMeterWarning(player.health / Mathf.Max(1, player.maxHealth), Time.unscaledTime);
            UpdateEnergyShield(healthLimit > 0, Time.unscaledTime);
            bool recharging = player.GetComponent<PassiveEnergyCore>() != null && !GameRules.playerDisabled && player.mana < player.maxMana;
            var energyColor = recharging ? Color.Lerp(Cyan, new Color(.55f, .95f, .82f), .3f + .3f * Mathf.Sin(Time.time * 4)) : Cyan;
            Fill(manaCells, player.mana / Mathf.Max(1, player.maxMana), energyColor, energyLimit);
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
        int challenge = waves != null ? waves.ChallengeWavesRemaining : 0;
        for (int i = 0; i < challengeCells.Length; i++)
            challengeCells[i].gameObject.SetActive(i < challenge);
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

    static Color PhaseColor(int phase) => phase == 1 ? Amber : phase == 2 ? new Color(1, .45f, .18f) : Alarm;

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
        // The Energy Shield wraps the last hit: a soft glow behind the bottom cell and a ring over its edge.
        if (shieldGlow == null)
        {
            shieldGlow = ShieldPart("Energy shield glow", 0);
            shieldRing = ShieldPart("Energy shield ring", 1.5f);
        }
        var bottom = healthCells[0].rectTransform;
        foreach (var part in new[] { shieldGlow, shieldRing })
            Place(part.rectTransform, bottom.anchoredPosition.x - 3, -bottom.anchoredPosition.y - 3, bottom.sizeDelta.x + 6, bottom.sizeDelta.y + 6);
        shieldGlow.transform.SetSiblingIndex(bottom.GetSiblingIndex());
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

    void UpdateEnergyShield(bool lit, float time)
    {
        var cell = player.GetComponent<EnergyShieldCell>();
        shieldGlow.enabled = shieldRing.enabled = lit && cell != null;
        if (!shieldRing.enabled)
            return;
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
