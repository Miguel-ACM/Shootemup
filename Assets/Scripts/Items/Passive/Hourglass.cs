using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class Hourglass : AItem
{
    void Awake()
    {
        SetData("Hourglass", "Time heals", "Damage builds while you avoid hits.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Hourglass);
        if (s.GetComponent<HourglassSand>() == null)
            s.gameObject.AddComponent<HourglassSand>();
        base.OnPickup();
    }
}

// +5% damage per copy for every 4 s of active play without a hit, up to +40% per copy.
// A thin arc of sand beneath the hull fills with the build-up; a hit spills it.
public sealed class HourglassSand : MonoBehaviour
{
    public const float StepSeconds = 4, StepBonus = .05f;
    public const int Steps = 8;
    const int ArcPoints = 13;
    const float ArcRadius = .62f, ArcSpan = 150;
    static readonly Color Sand = new Color(1, .8f, .38f);
    static HourglassSand current;
    Shipcontroller ship;
    LineRenderer arc;
    float clean;
    int shown = -1;

    static int Copies => CombatUpgrades.Current != null ? CombatUpgrades.Current.Count(UpgradeKind.Hourglass) : 0;

    int Filled => Mathf.Min(Steps, Mathf.FloorToInt(clean / StepSeconds));

    public static float Bonus => current != null ? current.Filled * StepBonus * Copies : 0;

    void Start()
    {
        current = this;
        ship = GetComponent<Shipcontroller>();
        ship.HitTaken += Spill;
        arc = CombatVisuals.Line(transform, new Color(Sand.r, Sand.g, Sand.b, .42f), .028f);
        arc.useWorldSpace = false;
        arc.sortingLayerName = "Player";
        arc.sortingOrder = -2;
        arc.enabled = false;
    }

    void OnDestroy()
    {
        if (ship != null)
            ship.HitTaken -= Spill;
        if (arc != null)
            Destroy(arc.gameObject);
        if (current == this)
            current = null;
    }

    void Spill()
    {
        if (Filled > 0)
        {
            ItemSpark.Emit(transform.position + Vector3.down * ArcRadius, Sand, 6, .09f);
            ItemSound.Cue("shatter", volume: .05f, pitch: 1.3f, cooldown: .3f);
        }
        clean = 0;
    }

    void Update()
    {
        if (Copies == 0)
        {
            arc.enabled = false;
            return;
        }
        int before = Filled;
        clean += GameClock.ActivePlayDelta;
        int filled = Filled;
        if (filled > before)
        {
            ItemSpark.Emit(transform.position + Vector3.down * ArcRadius, Sand, filled == Steps ? 5 : 2, .07f);
            if (filled == Steps)
            {
                ItemSound.Cue("ready", volume: .05f, pitch: 1.2f, cooldown: .5f);
                RunInventory.Ping(this);
            }
        }
        if (filled != shown)
            Draw(filled);
    }

    void Draw(int filled)
    {
        shown = filled;
        arc.enabled = filled > 0;
        if (filled == 0)
            return;
        float scale = Mathf.Max(.01f, transform.lossyScale.x);
        float span = ArcSpan * filled / Steps;
        arc.positionCount = ArcPoints;
        for (int i = 0; i < ArcPoints; i++)
        {
            float angle = (-90 - span * .5f + span * i / (ArcPoints - 1)) * Mathf.Deg2Rad;
            arc.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), -.1f) * (ArcRadius / scale));
        }
        arc.widthMultiplier = 1 / scale;
    }
}
