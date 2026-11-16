using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class HangarHub
{
    List<GameObject> pageNodes => pages[room];
    static readonly string[] categories = { "ALL PARTS", "PASSIVES", "ACTIVES", "PARTNERS", "GUNS", "SECONDARIES" };
    static readonly LevelId[] districts ={LevelId.Fields,LevelId.Snow,LevelId.City,LevelId.Thunder,LevelId.Desert,
        LevelId.Tunnel,LevelId.Candyland,LevelId.Space,LevelId.Underwater,LevelId.Sewers,LevelId.Hell,LevelId.Heaven,LevelId.Core,LevelId.Void};
    static readonly string[] districtMarks = { "PL", "SN", "CI", "TH", "DE", "TU", "CA", "SP", "UW", "SE", "HL", "HV", "CO", "VO" };
    static int Cycle(int n, int count) => count == 0 ? 0 : (n % count + count) % count;
    void Navigate(bool horizontal, int step)
    {
        if (room == (int)Dock.Armory)
        {
            if (horizontal)
                galleryCategory = band = Cycle(band + step, categories.Length);
            else
                galleryAvailability = selection = Cycle(selection + step, availability.Length);
            ApplyGalleryFilter();
            return;
        }
        if (!horizontal)
        {
            if (room == (int)Dock.Fleet)
                band = Cycle(band + step, 3);
            else if (room == (int)Dock.Recorder)
            {
                band = Cycle(band + step, 2);
                selection = 0;
            }
            else
                selection = Cycle(selection + step, room == (int)Dock.Simulator ? FlightTrials.All.Length : 3);
        }
        else
        {
            int count = room == (int)Dock.Fleet ? ShipFrames.Count :
                room == (int)Dock.Simulator ? FlightTrials.All.Length : room == (int)Dock.Flight ? 3 :
                MetaProgress.History.Count(f => string.IsNullOrEmpty(f.contract) == (band == 0));
            selection = Cycle(selection + step, count);
        }
        RenderPage();
    }
    TextMeshProUGUI Legend(string s, float x, float y, float width, float height, float size, Color color, bool bold = false)
    {
        var t = instrument.Label(s, x, y, width, height, size, color, bold);
        pageNodes.Add(t.gameObject);
        return t;
    }
    void Tile(float x, float y, float w, float h, Color color)
    {
        pageNodes.Add(DockInstrument.Block(instrument.face, "Inlay", new Vector2(x, y), new Vector2(w, h), color).gameObject);
    }
    void Photo(Texture texture, Rect uv, float x, float y, float size, Color tint)
    {
        var image = new GameObject("Live display", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        image.transform.SetParent(instrument.face, false);
        image.rectTransform.sizeDelta = Vector2.one * size;
        image.rectTransform.anchoredPosition = new Vector2(x, y);
        image.texture = texture;
        image.uvRect = uv;
        image.color = tint;
        image.raycastTarget = false;
        pageNodes.Add(image.gameObject);
    }
    TextMeshProUGUI consoleFooter;

    void RefreshConsoleFooter()
    {
        if (!operating) { consoleFooter.text = ""; return; }
        string back = DockInstrument.MenuKey(true);
        consoleFooter.text = (Dock)room switch
        {
            Dock.Armory => searchingGallery ? "ENTER  /  DONE" : DockInstrument.MenuKey() + " SEARCH   " + back + " EXIT",
            Dock.Fleet => InputPrompts.Navigation(false) + " HULL   " + InputPrompts.Navigation(true) + " MODE   " + back + " EXIT",
            Dock.Simulator => DockInstrument.MenuKey() + "  LAUNCH SIM   " + back + " RELEASE",
            Dock.Flight => DockInstrument.MenuKey() + "  SET   " + back + " RELEASE",
            _ => back + "  RELEASE"
        };
    }
    void RenderPage()
    {
        if (room == (int)Dock.Armory)
        {
            consoleFooter = directoryControls;
            RefreshConsoleFooter();
            return;
        }
        foreach (var node in pageNodes)
            if (node != null)
            {
                node.SetActive(false);
                Destroy(node);
            }
        pageNodes.Clear();
        bool dimScreen = false;
        Color accent = lamps[room];
        Legend((room + 1).ToString("00") + "  /  " + signs[room], -41, 255, 476, 34, 17, accent, true);
        switch ((Dock)room)
        {
            case Dock.Fleet:
                FleetPage();
                break;
            case Dock.Simulator:
                dimScreen = !TrialPage();
                break;
            case Dock.Recorder:
                HistoryPage();
                break;
            case Dock.Flight:
                FlightPage();
                break;
        }
        consoleFooter = Legend("", 0, -254, 558, 32, 17, DockInstrument.Paper);
        RefreshConsoleFooter();
        if (dimScreen)
            pageNodes.Add(DockInstrument.Block(instrument.face, "Locked tape dimmer", Vector2.zero, DockInstrument.TubeSize,
                new Color(0, 0, 0, .46f)).gameObject);
    }
    void FleetPage()
    {
        var hull = ShipFrames.Get((ShipFrame)selection);
        var record = MetaProgress.Record(hull.frame, modes[band]);
        bool open = MetaProgress.CanFly(hull.frame);
        if (!open)
            Tile(-193, 117, 165, 165, Metal);
        Photo(Resources.Load<Texture2D>("DockArt/Portrait" + hull.frame), new Rect(0, 0, 1, 1), -193, 117, 165, open ? Color.white : Color.black);
        Legend(hull.frame.ToString().ToUpperInvariant(), 76, 179, 350, 56, 41, DockInstrument.Paper, true);
        Legend(hull.hits + " HP  " + hull.moveSpeed.ToString("0.#") + " SPD", 76, 126, 350, 38, 21, lamps[room]);
        FleetEquipment(hull, open);
        if (!operating)
        {
            Legend(modes[band].ToString().ToUpperInvariant(), 0, -30, 558, 42, 24, lamps[room]);
            Legend(record.flights + " flights", 0, -86, 558, 48, 30, DockInstrument.Paper);
            Legend(MetaProgress.Ship == hull.frame ? "IN FLIGHT" : open ? "COMMISSIONED" : "Clear Void in " + (ShipFrame)(selection - 1),
                0, -177, 558, 40, 20, DockInstrument.Brass);
            return;
        }
        Legend(modes[band].ToString().ToUpperInvariant() + "  /  BOSS RECORD", 0, 26, 558, 36, 21, lamps[room], true);
        for (int i = 0; i < districts.Length; i++)
        {
            float x = -250 + i * (500f / (districts.Length - 1));
            int bit = 1 << (int)districts[i];
            bool clear = (record.clears & bit) != 0, clean = (record.flawless & bit) != 0;
            Tile(x, -22, 35, 43, DockInstrument.Ink);
            Photo(Resources.Load<Texture2D>("DockArt/Stamp" + districts[i]), new Rect(0, 0, 1, 1), x, -22, 33,
                clean ? DockInstrument.Brass : clear ? DockInstrument.Sea : new Color(.40f, .47f, .48f));
            Legend(districtMarks[i], x, -60, 50, 24, 17, DockInstrument.Paper).alignment = TextAlignmentOptions.Center;
        }
        Legend("GOLD = NO HIT", 0, -92, 558, 26, 15, DockInstrument.Brass);
        var captions = new[] { "FLIGHTS", "WINS", "BEST" };
        var values = new[] { record.flights.ToString(), record.victories.ToString(), record.best.ToString("N0") };
        for (int i = 0; i < captions.Length; i++)
        {
            float x = -184 + i * 184;
            Legend(captions[i], x, -132, 175, 28, 17, DockInstrument.Muted);
            Legend(values[i], x, -162, 175, 36, 24, DockInstrument.Paper, true);
        }
        string status = !open ? "Clear Void in " + (ShipFrame)(selection - 1) : MetaProgress.Ship == hull.frame ? "IN FLIGHT" : "READY TO FLY";
        Legend(status, 0, -197, 558, 28, 18, DockInstrument.Brass);
    }
    void FleetEquipment(ShipFrames.Definition hull, bool open)
    {
        var kinds = new List<string>();
        if (hull.primary != null) kinds.Add(hull.primary + "Item");
        kinds.AddRange(hull.starters);
        var studio = ItemPortraitStudio.Create(kinds, 128);
        studio.transform.SetParent(interiors[(int)Dock.Fleet].fixtures.transform, true);
        pageNodes.Add(studio.gameObject);
        float pitch = Mathf.Min(92, 350f / Mathf.Max(1, kinds.Count));
        for (int i = 0; i < kinds.Count; i++)
        {
            float x = 76 + (i - (kinds.Count - 1) * .5f) * pitch;
            Photo(studio.Atlas, studio.Cell(i), x, 73, 64, open ? Color.white : Color.black);
        }
    }
    bool TrialPage()
    {
        var trial = FlightTrials.All[selection];
        string lockReason = trial.LockReason;
        bool available = lockReason == null;
        var mode = trial.DifficultyFor(difficulty);
        bool completed = TrialCompleted(trial);
        Color accent = completed ? DockInstrument.Brass : lamps[room];
        if (completed)
        {
            Tile(-198, 113, 161, 161, accent);
            Tile(-198, 113, 155, 155, DockInstrument.Ink);
        }
        Photo(Resources.Load<Texture2D>("DockArt/Portrait" + trial.frame), new Rect(0, 0, 1, 1), -198, 113, 153,
            MetaProgress.CanFly(trial.frame) ? Color.white : Color.black);
        // The certification stamp sits inside the portrait's corner, clear of the objective below.
        if (completed)
        {
            Tile(-141, 70, 44, 44, accent);
            TrialCheck(new Vector2(-141, 70), 1.2f, DockInstrument.Ink);
        }
        Legend("TAPE " + (selection + 1).ToString("00") + " / " + FlightTrials.All.Length.ToString("00") + (available ? "" : "  LOCKED"),
            80, 196, 354, 29, 16, accent, true);
        Legend(trial.title, 80, 145, 354, 76, 35, completed ? accent : DockInstrument.Paper, true);
        Legend(trial.frame + "    /    " + mode, 80, 78, 354, 34, 21, DockInstrument.Muted);
        Legend(trial.objective, 0, operating ? 8 : -20, 558, 50, 23, DockInstrument.Paper);
        if (operating)
            TrialEquipment(trial, completed);
        if (completed && available)
        {
            Tile(0, -143, 558, 38, DockInstrument.Brass);
            TrialCheck(new Vector2(-251, -143), .85f, DockInstrument.Ink);
            Legend("CERTIFIED", 18, -143, 490, 32, 24, DockInstrument.Ink, true);
        }
        else
        {
            string status = lockReason ?? SimulatorFlight.Label(trial.approach);
            Legend(status, 0, -143, 558, 38, 19, available ? accent : DockInstrument.Brass, true);
        }
        TrialMarks();
        if (operating)
            Legend(InputPrompts.Navigation(false) + " CONTRACT", 0, -201, 558, 25, 15, DockInstrument.Muted);
        return available;
    }

    static bool TrialCompleted(FlightTrials trial) => MetaProgress.ContractClear(trial.CompletionKey(trial.DifficultyFor(difficulty)));

    static Color TrialItemTint(string key) =>
        ItemCatalog.TryGet(key, out var item) && item.InRewardPool && !MetaProgress.IsUnlocked(key) ? Color.black : Color.white;

    void TrialEquipment(FlightTrials trial, bool completed)
    {
        var kinds = trial.kit.Length == 0 ? new List<string> { "BaseWeaponItem" } : new List<string>(trial.kit);
        int kitCount = kinds.Count;
        bool reward = !string.IsNullOrEmpty(trial.reward);
        if (reward)
            kinds.Add(trial.reward);
        var studio = ItemPortraitStudio.Create(kinds, 128);
        // The page owns the camera as well as its pictures: replacing/closing the
        // page disables it immediately, then releases its atlas with the old nodes.
        studio.transform.SetParent(interiors[(int)Dock.Simulator].fixtures.transform, true);
        pageNodes.Add(studio.gameObject);
        float width = reward ? 414 : 558;
        float middle = reward ? -72 : 0;
        float pitch = Mathf.Min(106, width / kitCount);
        float size = Mathf.Min(76, pitch - 10);
        Legend("KIT", middle, -27, width, 17, 13, DockInstrument.Muted);
        for (int i = 0; i < kitCount; i++)
        {
            float x = middle + (i - (kitCount - 1) * .5f) * pitch;
            Tile(x, -111, size * .7f, 3, lamps[room]);
            Photo(studio.Atlas, studio.Cell(i), x, -76, size, TrialItemTint(kinds[i]));
        }
        if (reward)
        {
            Tile(154, -72, 2, 87, Metal);
            Legend(completed ? "EARNED" : "REWARD", 225, -27, 108, 17, 13, DockInstrument.Brass).alignment = TextAlignmentOptions.Center;
            Tile(225, -111, 58, 3, DockInstrument.Brass);
            Photo(studio.Atlas, studio.Cell(kitCount), 225, -76, 76, TrialItemTint(trial.reward));
        }
    }

    static readonly Color OpenTape = new Color(.42f, .86f, .5f);

    void TrialMarks()
    {
        // Brass tapes are cleared, green ones are open to fly, dark ones are still locked; the
        // underline is the current selection (Miguel, 15 November 2026).
        float pitch = Mathf.Min(23, 558f / FlightTrials.All.Length);
        for (int i = 0; i < FlightTrials.All.Length; i++)
        {
            float x = (i - (FlightTrials.All.Length - 1) * .5f) * pitch;
            bool completed = TrialCompleted(FlightTrials.All[i]);
            bool open = !completed && FlightTrials.All[i].LockReason == null;
            Tile(x, -178, pitch - 4, 9, completed ? DockInstrument.Brass : open ? OpenTape : Metal);
            if (!completed && !open)
                Tile(x, -178, pitch - 7, 5, DockInstrument.Ink);
            if (i == selection)
                Tile(x, -187, pitch - 4, 3, DockInstrument.Paper);
        }
    }

    void TrialCheck(Vector2 center, float scale, Color color)
    {
        TrialStroke(center + new Vector2(-9, 0) * scale, center + new Vector2(-2, -7) * scale, 3.5f * scale, color);
        TrialStroke(center + new Vector2(-2, -7) * scale, center + new Vector2(12, 9) * scale, 3.5f * scale, color);
    }

    void TrialStroke(Vector2 from, Vector2 to, float width, Color color)
    {
        var line = DockInstrument.Block(instrument.face, "Certification check", (from + to) * .5f, new Vector2(Vector2.Distance(from, to), width), color);
        line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg);
        pageNodes.Add(line.gameObject);
    }
    void HistoryPage()
    {
        bool sim = band == 1;
        var logs = MetaProgress.History.Where(f => string.IsNullOrEmpty(f.contract) != sim).ToList();
        Legend(sim ? "SIMULATOR TAPES" : "CAMPAIGN TAPES", 0, 196, 558, 39, 24, lamps[room], true);
        if (logs.Count == 0)
        {
            // The deck recorder's empty tape echoes the twin spools on the physical machine.
            Tile(0, 75, 212, 116, Metal);
            Tile(0, 77, 198, 103, DockInstrument.Ink);
            Tile(0, 84, 164, 43, Metal);
            for (int side = -1; side <= 1; side += 2)
            {
                Tile(58 * side, 84, 34, 29, lamps[room]);
                Tile(58 * side, 84, 16, 14, DockInstrument.Ink);
            }
            Tile(0, 44, 120, 8, DockInstrument.Muted);
            Legend("Blank tape", 0, -33, 558, 60, 35, DockInstrument.Paper).alignment = TextAlignmentOptions.Center;
            Legend("00:00:00", 0, -96, 558, 38, 21, lamps[room]).alignment = TextAlignmentOptions.Center;
        }
        else
        {
            var f = logs[selection];
            Legend(f.outcome.ToUpperInvariant() + "   /   " + (sim ? f.contract : f.frame.ToString()), 0, 140, 558, 62, 32, DockInstrument.Paper, true);
            Legend(f.difficulty + "    /    " + f.Duration + "    /    " + f.score.ToString("N0") + " pts", 0, 83, 558, 36, 21, lamps[room]);
            Legend(f.route, 0, 35, 558, 51, 20, DockInstrument.Paper);
            if (!operating)
                return;
            Legend(f.bosses + " bosses    " + f.clean + " no-hit    " + f.hits + " hits    +" + f.earned + " coins", 0, -27, 558, 46, 20, DockInstrument.Paper);
            var r = MetaProgress.Record(f.frame, f.difficulty);
            var blueprint = sim ? FlightTrials.Find(f.contract)?.reward : null;
            string details = sim ? blueprint == null ? "" : "Blueprint   /   " + FlightTrials.Display(blueprint) :
                "HULL TOTAL    /    " + r.deaths + " losses    " + r.coins + " coins    " + TimeSpan.FromSeconds(r.seconds).TotalHours.ToString("0.0") + " h";
            Legend(details, 0, -91, 558, 42, 17, DockInstrument.Muted);
            if (f.awards != null && f.awards.Count > 0)
                Legend(string.Join("   ·   ", f.awards), 0, -143, 558, 60, 15, DockInstrument.Brass);
        }
        if (operating)
            Legend(InputPrompts.Navigation(false) + " FLIGHT    " + InputPrompts.Navigation(true) + " LOG", 0, -189, 558, 33, 15, DockInstrument.Muted);
    }
    void FlightPage()
    {
        Legend("Flight plan", 0, 174, 558, 72, 40, DockInstrument.Paper, true);
        Legend(MetaProgress.Ship + "  /  PLAINS", 0, 100, 558, 42, 23, DockInstrument.Muted);
        if (!operating)
        {
            Legend(difficulty.ToString().ToUpperInvariant(), 0, -16, 558, 72, 38, lamps[room], true);
            Legend(factors[Array.IndexOf(modes, difficulty)] + " SCORE", 0, -87, 558, 42, 23, DockInstrument.Paper);
            return;
        }
        for (int i = 0; i < 3; i++)
        {
            float x = (i - 1) * 184;
            Tile(x, -10, 171, 99, i == selection ? Metal : DockInstrument.Ink);
            Legend(modes[i].ToString().ToUpperInvariant(), x, 6, 160, 37, 22, i == selection ? lamps[room] : DockInstrument.Muted, true).alignment = TextAlignmentOptions.Center;
            Legend(factors[i], x, -31, 160, 32, 21, DockInstrument.Paper).alignment = TextAlignmentOptions.Center;
            if (i == selection)
                Tile(x, -63, 160, 4, lamps[room]);
        }
        Legend("FILED " + difficulty.ToString().ToUpperInvariant() + "  /  " + MetaProgress.Coins + " banked", 0, -107, 558, 36, 18, lamps[room]);
        string kit = string.Join(", ", MetaProgress.Outbound.Select(FlightTrials.Display));
        Legend("KIT  " + (kit.Length == 0 ? "Standard issue" : kit), 0, -150, 558, 42, 19, DockInstrument.Paper);
        Legend(InputPrompts.Navigation(false) + " DIFFICULTY", 0, -189, 558, 33, 15, DockInstrument.Muted);
    }
}
