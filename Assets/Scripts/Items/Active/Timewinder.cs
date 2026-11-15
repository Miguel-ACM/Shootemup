using System.Collections.Generic;
using UnityEngine;

// Timewinder, a League of Legends nod (Ekko's echo and Chronobreak). While the rewind is
// ready, a teal echo of the ship trails three seconds behind it along a faint thread of its
// path. Pressing it sends the ship racing back along that path to the echo, untouchable;
// hits taken since then are undone, and the arrival bursts, hurting enemies and erasing
// nearby fire.
[UnityEngine.Scripting.Preserve]
public sealed class Timewinder : OffensiveActiveItem
{
    public const float Window = 3, SampleRate = 30;
    const int Samples = (int)(Window * SampleRate);
    readonly Vector3[] path = new Vector3[Samples];
    readonly float[] hits = new float[Samples];
    int head, filled;
    float sampleClock;
    TimewinderEcho echo;

    void Awake()
    {
        SetData("Timewinder", "Wrong timeline", "Rewind to your echo, undoing hits taken since. The arrival bursts. 60 energy.", ItemType.active);
        cost = 60;
        cd = 6;
    }

    bool Equipped
    {
        get
        {
            var loadout = s != null && Collected ? s.GetComponent<ShipLoadout>() : null;
            return loadout != null && System.Array.IndexOf(loadout.Actives, this) >= 0;
        }
    }

    bool Ready => lastCD >= cd && s.mana >= EffectivePressCost && filled >= SampleRate;

    // The i-th recorded sample, oldest first.
    int Index(int i) => (head - filled + i + Samples * 2) % Samples;

    protected override void Update()
    {
        base.Update();
        if (!Equipped || s.health <= 0)
        {
            echo?.Hide();
            return;
        }
        // A new district or a hangar flight starts a fresh recording.
        if (CloudTransition.Busy || HangarHub.Active)
            filled = 0;
        if (Cast == null && GameClock.ActivePlay)
        {
            sampleClock += GameClock.PlayerDelta;
            while (sampleClock >= 1 / SampleRate)
            {
                sampleClock -= 1 / SampleRate;
                path[head] = s.transform.position;
                hits[head] = s.health;
                head = (head + 1) % Samples;
                filled = Mathf.Min(filled + 1, Samples);
            }
        }
        if (echo == null)
            echo = TimewinderEcho.Create(s);
        if (Cast == null && filled > 0 && Ready)
            echo.Show(path, Index(0), filled, s.transform.position);
        else if (Cast == null)
            echo.Hide();
    }

    protected override OffensiveCast Launch()
    {
        if (filled < SampleRate)
            return null;
        var route = new List<Vector3>(filled);
        for (int i = filled - 1; i >= 0; i--)
            route.Add(path[Index(i)]);
        float healthThen = hits[Index(0)];
        filled = 0;
        var rewind = CreateCast<TimewinderRewind>("Timewinder rewind");
        rewind.Prepare(route, healthThen, echo);
        return rewind;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (echo != null)
            Destroy(echo.gameObject);
    }
}

// The ship races back along its recorded path (newest to oldest), leaving afterimages, while
// a clock face winds backwards over the echo. On arrival the lost hits come back and the
// echo detonates.
public sealed class TimewinderRewind : OffensiveCast
{
    const float Travel = .42f, Linger = .45f, Radius = 2, AfterimageGap = .045f;
    static readonly Color Teal = new Color(.25f, .95f, .85f);
    static readonly Color Pale = new Color(.75f, 1f, .95f);
    List<Vector3> route;
    float healthThen, nextAfterimage;
    TimewinderEcho echo;
    LineRenderer dial, longHand, shortHand;
    Vector3 destination, target;
    bool arrived;

    public void Prepare(List<Vector3> path, float health, TimewinderEcho source)
    {
        route = path;
        healthThen = health;
        echo = source;
        destination = route[route.Count - 1];
    }

    protected override void Begin()
    {
        Owner.gameObject.AddComponent<ChronoShift>();
        target = Owner.transform.position;
        echo?.Flare();
        dial = CombatVisuals.Line(transform, new Color(Teal.r, Teal.g, Teal.b, .7f), .045f);
        longHand = CombatVisuals.Line(transform, Pale, .05f);
        shortHand = CombatVisuals.Line(transform, Pale, .065f);
        CombatVisuals.Ring(dial, destination, .85f);
        ItemSound.Cue("timeStop", volume: .16f, pitch: 1.45f);
        ItemSound.Cue("roll", volume: .1f, pitch: .7f);
    }

    protected override void Step(float dt)
    {
        // The hands spin backwards over the destination, a full turn per rewind.
        float wind = -Age / Travel * 360;
        Hand(longHand, wind, .75f);
        Hand(shortHand, wind / 12 + 120, .45f);
        if (!arrived)
        {
            float p = Mathf.Clamp01(Age / Travel);
            float eased = p * p * (3 - 2 * p);
            float along = eased * (route.Count - 1);
            int i = Mathf.Min(route.Count - 2, Mathf.FloorToInt(along));
            target = Vector3.Lerp(route[i], route[i + 1], along - i);
            target.z = Owner.transform.position.z;
            if (Age >= nextAfterimage)
            {
                nextAfterimage = Age + AfterimageGap;
                TimewinderEcho.Afterimage(Owner, Owner.transform.position);
            }
            if (p >= 1)
                Arrive();
            return;
        }
        float fade = 1 - Mathf.Clamp01((Age - Travel) / Linger);
        dial.startColor = dial.endColor = new Color(Teal.r, Teal.g, Teal.b, .7f * fade);
        longHand.startColor = longHand.endColor = shortHand.startColor = shortHand.endColor = new Color(Pale.r, Pale.g, Pale.b, fade);
        if (Age >= Travel + Linger)
            Finish();
    }

    void LateUpdate()
    {
        // Applied after the ship's own movement so the rewind owns its position.
        if (!arrived && Owner != null)
            Owner.transform.position = target;
    }

    void Hand(LineRenderer hand, float degrees, float length)
    {
        var tip = destination + (Vector3)(CombatVisuals.Rotate(Vector2.up, degrees) * length);
        hand.positionCount = 2;
        hand.SetPosition(0, destination + Vector3.back * .15f);
        hand.SetPosition(1, tip + Vector3.back * .15f);
    }

    void Arrive()
    {
        arrived = true;
        // Walls scroll on: land on the latest point of the path that is still open.
        for (int i = route.Count - 1; i >= 0 && !Owner.TryReturnTo(route[i]); i--)
        {
        }
        var at = Owner.transform.position;
        int restored = Mathf.Max(0, Mathf.RoundToInt(healthThen - Owner.health));
        if (restored > 0)
        {
            Owner.Repair(restored);
            ItemSpark.Emit(at, Pale, 10, .12f);
        }
        float damage = GameRules.playerShootDamage * 10;
        var struck = new List<AEnemy>();
        foreach (var enemy in AEnemy.Live)
            if (enemy != null && enemy.CanBeTargeted && enemy.CanReceiveDamage &&
                enemy.ClosestDamagePoint(at, out Vector2 point) && ((Vector2)at - point).sqrMagnitude <= Radius * Radius)
                struck.Add(enemy);
        foreach (var enemy in struck)
            if (enemy != null)
            {
                enemy.ClosestDamagePoint(at, out Vector2 point);
                CombatHit.Apply(enemy, damage, point, (point - (Vector2)at).normalized, generation: 1);
            }
        var erased = new List<CombatRound>();
        foreach (var round in CombatRound.Live)
            if (BulletReflection.CanReflect(round) && ((Vector2)(round.transform.position - at)).sqrMagnitude <= Radius * Radius)
                erased.Add(round);
        foreach (var round in erased)
            round.Vanish();
        PSXFireballs.Shockwave(at, Radius * 1.6f, .55f, Teal, 3, .8f);
        ItemProcVfx.Emit(ItemProcShape.Return, at, Teal, .9f);
        ItemSpark.Emit(at, Teal, 12, .14f);
        ItemSound.Cue("itemChain", volume: .14f, pitch: .75f);
        if (EZCameraShake.CameraShaker.Instance != null)
            EZCameraShake.CameraShaker.Instance.ShakeOnce(1.2f, 6, .05f, .35f);
        // A short grace after landing, then the ship is hittable again.
        Owner.GetComponent<ChronoShift>()?.Release(.25f);
    }

    void OnDestroy()
    {
        if (Owner != null && Owner.TryGetComponent<ChronoShift>(out var shift) && !arrived)
            Destroy(shift);
    }
}

// Present on the ship while it is being rewound: the ship cannot be hit.
public sealed class ChronoShift : MonoBehaviour
{
    float release = -1;

    public void Release(float after)
    {
        release = after;
    }

    void Update()
    {
        if (release < 0)
            return;
        release -= Time.deltaTime;
        if (release <= 0)
            Destroy(this);
    }
}

// The teal hologram of the ship: the waiting echo, and short-lived afterimages of a rewind.
public sealed class TimewinderEcho : MonoBehaviour
{
    static Material material;
    readonly List<MeshRenderer> parts = new List<MeshRenderer>();
    MaterialPropertyBlock properties;
    LineRenderer thread;
    float flare, life = -1, age, shown;
    bool visible;

    static Material Material => material != null ? material
        : material = new Material(Resources.Load<Shader>("TimewinderEcho")) { name = "Timewinder echo hologram" };

    public static TimewinderEcho Create(Shipcontroller ship)
    {
        var echo = Build(ship, "Timewinder echo");
        echo.thread = CombatVisuals.Line(null, new Color(.25f, .95f, .85f, .22f), .03f);
        echo.thread.transform.SetParent(echo.transform, false);
        echo.thread.useWorldSpace = true;
        echo.Hide();
        return echo;
    }

    public static void Afterimage(Shipcontroller ship, Vector3 at)
    {
        var ghost = Build(ship, "Timewinder afterimage");
        ghost.transform.position = at;
        ghost.life = .35f;
        ghost.visible = true;
        ghost.shown = .8f;
    }

    static TimewinderEcho Build(Shipcontroller ship, string title)
    {
        var go = new GameObject(title);
        go.transform.position = ship.transform.position;
        var echo = go.AddComponent<TimewinderEcho>();
        echo.properties = new MaterialPropertyBlock();
        var visual = ship.GetComponent<PSXShipVisual>();
        var source = visual != null ? visual.Model : ship.transform;
        foreach (var mesh in source.GetComponentsInChildren<MeshFilter>())
        {
            var renderer = mesh.GetComponent<MeshRenderer>();
            if (mesh.sharedMesh == null || renderer == null || !renderer.enabled)
                continue;
            var part = new GameObject(mesh.name);
            part.transform.SetParent(go.transform, false);
            part.transform.position = mesh.transform.position;
            part.transform.rotation = mesh.transform.rotation;
            part.transform.localScale = mesh.transform.lossyScale;
            part.AddComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
            var copy = part.AddComponent<MeshRenderer>();
            var materials = new Material[mesh.sharedMesh.subMeshCount];
            for (int i = 0; i < materials.Length; i++)
                materials[i] = Material;
            copy.sharedMaterials = materials;
            copy.sortingLayerName = "Player";
            copy.sortingOrder = -1;
            echo.parts.Add(copy);
        }
        return echo;
    }

    /// <summary>Places the echo at the oldest recorded point, with a thread along the path to the ship.</summary>
    public void Show(Vector3[] path, int oldest, int count, Vector3 ship)
    {
        visible = true;
        var at = path[oldest];
        transform.position = new Vector3(at.x, at.y, ship.z + .05f);
        const int Points = 16;
        thread.positionCount = Points;
        for (int i = 0; i < Points; i++)
        {
            var p = path[(oldest + Mathf.RoundToInt((count - 1) * i / (float)(Points - 1))) % path.Length];
            thread.SetPosition(i, new Vector3(p.x, p.y, ship.z + .1f));
        }
    }

    public void Hide()
    {
        visible = false;
    }

    public void Flare()
    {
        flare = 1;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        age += dt;
        if (life > 0)
        {
            shown = .8f * Mathf.Clamp01(1 - age / life);
            if (age >= life)
            {
                Destroy(gameObject);
                return;
            }
        }
        else
            shown = Mathf.MoveTowards(shown, visible ? 1 : 0, dt * (visible ? 3 : 6));
        flare = Mathf.MoveTowards(flare, 0, dt * 2.5f);
        properties.SetFloat("_Fade", shown);
        properties.SetFloat("_Phase", age);
        properties.SetFloat("_Flare", flare);
        foreach (var part in parts)
        {
            part.enabled = shown > .01f;
            part.SetPropertyBlock(properties);
        }
        if (thread != null)
        {
            thread.enabled = shown > .01f;
            var c = new Color(.25f, .95f, .85f, .22f * shown);
            // Brightest at the echo, thinning out towards the ship.
            thread.startColor = c;
            thread.endColor = new Color(c.r, c.g, c.b, 0);
        }
    }
}
