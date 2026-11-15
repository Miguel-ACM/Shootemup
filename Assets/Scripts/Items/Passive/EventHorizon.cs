using UnityEngine;

// Event Horizon: space bends around the cockpit. Hostile rounds closing on it drift sideways,
// across their own path, so many near misses become misses. Rounds already past the ship are
// left alone and beams are not bent; a round aimed dead centre still needs a moment to slide
// off, so it is a help rather than a shield.
[UnityEngine.Scripting.Preserve]
public sealed class EventHorizon : AItem
{
    public const float Radius = .95f, Strength = 1.4f;
    static Vector2 centre;
    static int copies;

    public static bool Active { get; private set; }

    void Awake()
    {
        SetData("Event Horizon", "Space bends", "Enemy fire bends away from your cockpit.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.EventHorizon);
        if (s.GetComponent<EventHorizonField>() == null)
            s.gameObject.AddComponent<EventHorizonField>();
        base.OnPickup();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetField()
    {
        Active = false;
    }

    public static void Hold(Vector2 at, int count)
    {
        centre = at;
        copies = count;
        Active = count > 0;
    }

    public static void Release()
    {
        Active = false;
    }

    /// <summary>The sideways drift for a round at p travelling with the given velocity.</summary>
    public static Vector3 Bend(Vector3 p, Vector2 velocity, int serial)
    {
        var offset = (Vector2)p - centre;
        float distance = offset.magnitude;
        if (distance >= Radius || velocity.sqrMagnitude < .0001f)
            return Vector3.zero;
        var heading = velocity.normalized;
        // Only rounds still approaching the cockpit are bent.
        if (Vector2.Dot(offset, heading) > 0)
            return Vector3.zero;
        var across = offset - heading * Vector2.Dot(offset, heading);
        if (across.sqrMagnitude < .0004f)
            across = Vector2.Perpendicular(heading) * (serial % 2 == 0 ? 1 : -1);
        float falloff = 1 - distance / Radius;
        return across.normalized * Strength * (1 + .3f * (copies - 1)) * falloff * falloff;
    }
}

// Keeps the field on the cockpit during live combat, with a faint violet ring at its edge.
public sealed class EventHorizonField : MonoBehaviour
{
    static readonly Color Rim = new Color(.55f, .4f, .85f, .2f);
    LineRenderer ring;
    Shipcontroller ship;

    void Start()
    {
        ship = GetComponent<Shipcontroller>();
        ring = CombatVisuals.Line(transform, Rim, .03f);
        ring.useWorldSpace = true;
        ring.sortingLayerName = "Player";
        ring.sortingOrder = -2;
    }

    void OnDestroy()
    {
        EventHorizon.Release();
        if (ring != null)
            Destroy(ring.gameObject);
    }

    void LateUpdate()
    {
        int copies = CombatUpgrades.Current != null ? CombatUpgrades.Current.Count(UpgradeKind.EventHorizon) : 0;
        bool live = copies > 0 && ship != null && ship.health > 0 && !GameRules.playerDisabled;
        ring.enabled = live && GameClock.ActivePlay;
        if (!live)
        {
            EventHorizon.Release();
            return;
        }
        EventHorizon.Hold(transform.position, copies);
        CombatVisuals.Ring(ring, transform.position, EventHorizon.Radius * (1 + .03f * Mathf.Sin(Time.time * 2.3f)));
    }
}
