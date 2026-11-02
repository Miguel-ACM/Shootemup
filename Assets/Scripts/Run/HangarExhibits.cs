using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class HangarHub
{
    sealed class Card
    {
        public ItemDefinition item;
        public string name, description;
        public AItem.ItemType kind;
        public int portrait;
    }
    readonly List<Card> catalog = new List<Card>(), shelf = new List<Card>();
    readonly List<GameObject> pageNodes = new List<GameObject>();
    ItemPortraitStudio portraits;
    GameObject turntable;
    RenderTexture hullPhoto;
    static readonly string[] categories = { "ALL PARTS", "PASSIVES", "ACTIVES", "PARTNERS", "GUNS", "SECONDARIES" };
    static readonly LevelId[] districts ={LevelId.Fields,LevelId.Snow,LevelId.City,LevelId.Thunder,LevelId.Desert,
        LevelId.Tunnel,LevelId.Candyland,LevelId.Space,LevelId.Underwater,LevelId.Hell};
    static readonly string[] districtMarks = { "PL", "SN", "CI", "TH", "DE", "TU", "CA", "SP", "UW", "HE" };
    void PrepareCatalog()
    {
        if (catalog.Count == 0)
        {
            foreach (var entry in ItemCatalog.All)
            {
                var probe = new GameObject("Read equipment card");
                var item = entry.Attach(probe);
                item.enabled = false;
                var data = item.GetData();
                catalog.Add(new Card { item = entry, name = data[0], description = data[2], kind = item.Kind });
                Destroy(probe);
            }
            catalog.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
            for (int i = 0; i < catalog.Count; i++)
                catalog[i].portrait = i;
        }
        portraits = ItemPortraitStudio.Create(catalog.Select(c => c.item.Key).ToList());
        FilterShelf();
    }
    void FilterShelf()
    {
        shelf.Clear();
        foreach (var c in catalog)
            if (band == 0 || (int)c.kind == band - 1)
                shelf.Add(c);
    }
    static int Cycle(int n, int count) => count == 0 ? 0 : (n % count + count) % count;
    void Navigate(bool horizontal, int step)
    {
        if (!horizontal)
        {
            if (room == (int)Dock.Armory)
            {
                band = Cycle(band + step, categories.Length);
                selection = 0;
                FilterShelf();
            }
            else if (room == (int)Dock.Fleet)
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
            int count = room == (int)Dock.Fleet ? ShipFrames.Count : room == (int)Dock.Armory ? shelf.Count :
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
    void ReleaseHull()
    {
        if (turntable != null)
        {
            turntable.SetActive(false);
            Destroy(turntable);
        }
        if (hullPhoto != null)
        {
            hullPhoto.Release();
            Destroy(hullPhoto);
        }
    }
    void ReleaseExhibit()
    {
        ReleaseHull();
        if (portraits != null)
            Destroy(portraits.gameObject);
    }
    void HullPortrait(ShipFrame frame, float x, float y, float size)
    {
        turntable = new GameObject("Inspection turntable");
        turntable.transform.position = new Vector3(-1060, 0, 0);
        var hull = ShipFrames.BuildDisplay(player, ShipFrames.Get(frame));
        if (hull == null)
            return;
        hull.transform.SetParent(turntable.transform, false);
        hull.transform.localPosition = Vector3.zero;
        hull.transform.localRotation = Quaternion.Euler(-12, 12, -12);
        var bounds = new Bounds();
        bool first = true;
        foreach (var r in hull.GetComponentsInChildren<MeshRenderer>())
        {
            if (first)
                bounds = r.bounds;
            else
                bounds.Encapsulate(r.bounds);
            first = false;
        }
        var c = new GameObject("Inspection lens").AddComponent<UnityEngine.Camera>();
        c.transform.SetParent(turntable.transform, false);
        c.transform.position = bounds.center + Vector3.back * 10;
        c.orthographic = true;
        c.orthographicSize = Mathf.Max(.15f, Mathf.Max(bounds.size.x, bounds.size.y) * .59f);
        c.aspect = 1;
        c.nearClipPlane = .1f;
        c.farClipPlane = 24;
        c.backgroundColor = Color.clear;
        c.clearFlags = CameraClearFlags.SolidColor;
        hullPhoto = new RenderTexture(192, 192, 16, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Point };
        c.targetTexture = hullPhoto;
        c.depth = -48;
        Photo(hullPhoto, new Rect(0, 0, 1, 1), x, y, size, Color.white);
    }
    void RenderPage()
    {
        foreach (var node in pageNodes)
            if (node != null)
            {
                node.SetActive(false);
                Destroy(node);
            }
        pageNodes.Clear();
        ReleaseHull();
        Color accent = lamps[room];
        Legend((room + 1).ToString("00") + "  /  " + signs[room], -41, 255, 476, 34, 17, accent, true);
        string action = "";
        switch ((Dock)room)
        {
            case Dock.Fleet:
                action = FleetPage();
                break;
            case Dock.Armory:
                ArchivePage();
                break;
            case Dock.Simulator:
                TrialPage();
                action = "LAUNCH SIM";
                break;
            case Dock.Recorder:
                HistoryPage();
                break;
            case Dock.Flight:
                FlightPage();
                action = "SET";
                break;
        }
        string keys = (action.Length > 0 ? DockInstrument.FireKey() + "  " + action + "      " : "") + DockInstrument.FireKey(true) + "  BACK";
        Legend(keys, 0, -254, 558, 32, 17, DockInstrument.Paper);
    }
    string FleetPage()
    {
        var hull = ShipFrames.Get((ShipFrame)selection);
        var record = MetaProgress.Record(hull.frame, modes[band]);
        HullPortrait(hull.frame, -193, 117, 157);
        Legend(hull.frame.ToString().ToUpperInvariant(), 76, 179, 350, 56, 41, DockInstrument.Paper, true);
        Legend(hull.hits + " HULL     " + hull.moveSpeed.ToString("0.#") + " SPEED", 76, 126, 350, 38, 21, lamps[room]);
        string gun = hull.primary == "BaseWeapon" ? "Twin cannon" : hull.primary == null ? "Unarmed" : FlightTrials.Display(hull.primary);
        Legend(gun + (hull.starters.Length > 0 ? "   /   " + string.Join(", ", hull.starters.Select(FlightTrials.Display)) : ""), 76, 80, 350, 60, 18, DockInstrument.Muted);
        var rows = modes.Select(mode => MetaProgress.Record(hull.frame, mode)).ToArray();
        for (int i = 0; i < districts.Length; i++)
        {
            float x = -168 + i * 48;
            Legend(districtMarks[i], x, 29, 43, 22, 15, DockInstrument.Muted, true).alignment = TextAlignmentOptions.Center;
            var stamp = Resources.Load<Texture2D>("DockArt/Stamp" + districts[i]);
            for (int m = 0; m < modes.Length; m++)
            {
                var row = rows[m];
                int bit = 1 << (int)districts[i];
                float y = 2 - m * 24;
                bool clear = (row.clears & bit) != 0, clean = (row.flawless & bit) != 0;
                if (i == 0)
                    Legend(modes[m].ToString(), -233, y, 88, 24, 16, m == band ? lamps[room] : DockInstrument.Muted, true);
                Tile(x, y, 40, 21, m == band ? Metal : DockInstrument.Ink);
                Photo(stamp, new Rect(0, 0, 1, 1), x, y, 22, clean ? DockInstrument.Brass : clear ? DockInstrument.Sea : new Color(.34f, .41f, .43f));
                if (clean)
                    Legend("*", x + 14, y, 13, 20, 15, DockInstrument.Brass, true);
            }
        }
        Legend(record.flights + " flights     " + record.victories + " victories     Best  " + record.best.ToString("N0"), 0, -86, 558, 34, 20, DockInstrument.Paper);
        bool open = MetaProgress.CanFly(hull.frame);
        string status = !open ? "LOCKED    /    Clear Hell with " + (ShipFrame)(selection - 1) : MetaProgress.Ship == hull.frame ? "ON THE LAUNCH LINE" : "CLEARED FOR FLIGHT";
        Legend(status, 0, -130, 558, 50, 22, DockInstrument.Brass, true);
        Legend("← →  HULL       ↑ ↓  DIFFICULTY      *  NO-HIT", 0, -189, 558, 33, 15, DockInstrument.Muted);
        return open && MetaProgress.Ship != hull.frame ? "FLY THIS HULL" : "";
    }
    void ArchivePage()
    {
        if (shelf.Count == 0)
            return;
        var c = shelf[selection];
        Legend(categories[band] + "      /      " + (selection + 1).ToString("00") + " OF " + shelf.Count.ToString("00"), 0, 198, 558, 40, 20, lamps[room], true);
        for (int i = 0; i < 7; i++)
        {
            int index = selection / 7 * 7 + i;
            if (index >= shelf.Count)
                break;
            float x = -237 + i * 79;
            Tile(x, 116, 70, 73, index == selection ? Metal : DockInstrument.Ink);
            var part = shelf[index];
            Photo(portraits.Atlas, portraits.Cell(part.portrait), x, 116, 67, Color.white);
            if (index == selection)
                Tile(x, 76, 66, 4, lamps[room]);
        }
        Legend(c.name, 0, 37, 558, 53, 34, DockInstrument.Paper, true);
        Legend(c.description, 0, -35, 558, 87, 22, DockInstrument.Paper);
        var trial = FlightTrials.Blueprint(c.item.Key);
        string source = c.item.Rarity == ItemRarity.Pact ? "Pact encounter" : c.item.Id == ItemId.LargeHealthKit ? "Low-hull shop stock" :
            c.item.Id == ItemId.ExplodingShield ? "Issued at every launch" : MetaProgress.IsUnlocked(c.item.Key) ? "Released  /  Run rewards and shops" :
            trial != null ? "Locked  /  Certify " + trial.title : "Locked  /  Buy on the launch lane for " + MetaProgress.Price(c.item.RarityKey) + " coins";
        Legend(source, 0, -131, 558, 56, 19, lamps[room], true);
        Legend("← →  PART       ↑ ↓  CATEGORY", 0, -189, 558, 33, 15, DockInstrument.Muted);
    }
    void TrialPage()
    {
        var trial = FlightTrials.All[selection];
        HullPortrait(trial.frame, -198, 113, 153);
        Legend("SIM TAPE   " + (selection + 1).ToString("00") + " / 04", 80, 196, 354, 29, 16, lamps[room], true);
        Legend(trial.title, 80, 145, 354, 76, 35, DockInstrument.Paper, true);
        Legend(trial.frame + "    /    " + trial.difficulty, 80, 78, 354, 34, 21, DockInstrument.Muted);
        Legend(trial.objective, 0, 16, 558, 57, 25, DockInstrument.Paper);
        Legend("LOAN KIT     " + string.Join("  /  ", trial.kit.Select(FlightTrials.Display)), 0, -54, 558, 58, 19, DockInstrument.Muted);
        Legend((MetaProgress.ContractClear(trial.title) ? "CERTIFIED" : "FIRST CLEAR") + "     /     " + FlightTrials.Display(trial.reward), 0, -129, 558, 54, 21, lamps[room], true);
        Legend("← →  CONTRACT", 0, -189, 558, 33, 15, DockInstrument.Muted);
    }
    void HistoryPage()
    {
        bool sim = band == 1;
        var logs = MetaProgress.History.Where(f => string.IsNullOrEmpty(f.contract) != sim).ToList();
        Legend(sim ? "SIMULATOR TAPES" : "CAMPAIGN TAPES", 0, 196, 558, 39, 24, lamps[room], true);
        if (logs.Count == 0)
            Legend("Awaiting first flight", 0, 48, 558, 80, 32, DockInstrument.Paper);
        else
        {
            var f = logs[selection];
            Legend(f.outcome.ToUpperInvariant() + "   /   " + (sim ? f.contract : f.frame.ToString()), 0, 140, 558, 62, 32, DockInstrument.Paper, true);
            Legend(f.difficulty + "    /    " + f.Duration + "    /    " + f.score.ToString("N0") + " pts", 0, 83, 558, 36, 21, lamps[room]);
            Legend(f.route, 0, 35, 558, 51, 20, DockInstrument.Paper);
            Legend(f.bosses + " bosses    " + f.clean + " no-hit    " + f.hits + " hits    +" + f.earned + " coins", 0, -27, 558, 46, 20, DockInstrument.Paper);
            var r = MetaProgress.Record(f.frame, f.difficulty);
            string details = sim ? "Blueprint   /   " + FlightTrials.Display(FlightTrials.All.First(t => t.title == f.contract).reward) :
                "HULL TOTAL    /    " + r.deaths + " losses    " + r.coins + " coins    " + TimeSpan.FromSeconds(r.seconds).TotalHours.ToString("0.0") + " h";
            Legend(details, 0, -91, 558, 42, 17, DockInstrument.Muted);
            if (f.awards != null && f.awards.Count > 0)
                Legend(string.Join("   ·   ", f.awards), 0, -143, 558, 60, 15, DockInstrument.Brass);
        }
        Legend("← →  FLIGHT       ↑ ↓  CAMPAIGN / SIM", 0, -189, 558, 33, 15, DockInstrument.Muted);
    }
    void FlightPage()
    {
        Legend("Departure manifest", 0, 174, 558, 72, 40, DockInstrument.Paper, true);
        Legend("HULL     " + MetaProgress.Ship + "       /       PLAINS", 0, 100, 558, 42, 23, DockInstrument.Muted);
        for (int i = 0; i < 3; i++)
        {
            float x = (i - 1) * 184;
            Tile(x, -10, 171, 99, i == selection ? Metal : DockInstrument.Ink);
            Legend(modes[i].ToString().ToUpperInvariant(), x, 6, 160, 37, 22, i == selection ? lamps[room] : DockInstrument.Muted, true).alignment = TextAlignmentOptions.Center;
            Legend(factors[i], x, -31, 160, 32, 21, DockInstrument.Paper).alignment = TextAlignmentOptions.Center;
            if (i == selection)
                Tile(x, -63, 160, 4, lamps[room]);
        }
        Legend("FILED   " + difficulty.ToString().ToUpperInvariant() + "     /     " + MetaProgress.Coins + " banked", 0, -107, 558, 36, 18, lamps[room]);
        string kit = string.Join(", ", MetaProgress.Outbound.Select(FlightTrials.Display));
        Legend("Outbound kit   /   " + (kit.Length == 0 ? "Standard issue" : kit), 0, -150, 558, 42, 19, DockInstrument.Paper);
        Legend("← →  DIFFICULTY", 0, -189, 558, 33, 15, DockInstrument.Muted);
    }
}
