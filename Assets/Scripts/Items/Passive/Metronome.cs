using UnityEngine;

// Metronome: hits that land on the beat of the music strike twice as hard. A thin ring ticks
// out from the ship on every beat so the rhythm can be felt, and on-beat hits flash white-gold.
// Without analysed music (or between tracks) it keeps time at 120 beats per minute.
[UnityEngine.Scripting.Preserve]
public sealed class Metronome : AItem
{
    public const float Window = .09f, Bonus = 1;
    static readonly Color Flash = new Color(1, .95f, .75f);

    void Awake()
    {
        SetData("Metronome", "Keep time", "Hits on the beat of the music strike twice as hard.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Metronome);
        if (s.GetComponent<MetronomeTick>() == null)
            s.gameObject.AddComponent<MetronomeTick>();
        base.OnPickup();
    }

    /// <summary>Seconds from now to the nearest beat: the music's own beats when known.</summary>
    public static float BeatOffset()
    {
        float offset = BGMManager.BeatOffset;
        if (!float.IsNaN(offset))
            return offset;
        const float Period = .5f;
        float phase = Mathf.Repeat(Time.time, Period);
        return phase < Period * .5f ? -phase : Period - phase;
    }

    public static float Scale(CombatUpgrades upgrades, Vector3 at)
    {
        int copies = upgrades.Count(UpgradeKind.Metronome);
        if (copies == 0 || Mathf.Abs(BeatOffset()) > Window)
            return 1;
        if (upgrades.Gate("metronome", .06f))
        {
            RunInventory.Ping(UpgradeKind.Metronome);
            ItemSpark.Emit(at, Flash, 3, .08f);
        }
        return 1 + Bonus * copies;
    }
}

// A faint ring that pulses out from the ship on every beat.
public sealed class MetronomeTick : MonoBehaviour
{
    static readonly Color Ink = new Color(1, .95f, .8f);
    LineRenderer ring;
    float previous = float.NaN, pulse = 1;

    void Start()
    {
        ring = CombatVisuals.Line(transform, Ink, .025f);
        ring.useWorldSpace = true;
        ring.sortingLayerName = "Player";
        ring.sortingOrder = -2;
        ring.enabled = false;
    }

    void OnDestroy()
    {
        if (ring != null)
            Destroy(ring.gameObject);
    }

    void Update()
    {
        bool owned = CombatUpgrades.Current != null && CombatUpgrades.Current.Count(UpgradeKind.Metronome) > 0;
        if (!owned || !GameClock.ActivePlay)
        {
            ring.enabled = false;
            return;
        }
        float offset = Metronome.BeatOffset();
        // The offset to the nearest beat crosses from positive to negative on the beat itself.
        if (!float.IsNaN(previous) && previous > 0 && offset <= 0)
            pulse = 0;
        previous = offset;
        pulse = Mathf.Min(1, pulse + Time.deltaTime * 3.5f);
        ring.enabled = pulse < 1;
        if (!ring.enabled)
            return;
        CombatVisuals.Ring(ring, transform.position, Mathf.Lerp(.45f, .95f, pulse));
        var c = Ink;
        c.a = .35f * (1 - pulse);
        ring.startColor = ring.endColor = c;
    }
}
