using UnityEngine;

// An indestructible laser drone deployed by the Thunder boss. It holds a station
// (fixed or patrolling), keeps one charged attached beam, and can shut down on a
// schedule: the beam ceases, the drone turns its emitter upward to show the lane is
// open, then turns back and recharges with a visible warning before firing again.
// It is not an enemy: player fire passes through it and it never collides.
public sealed class ThunderLaserBot : MonoBehaviour
{
    public enum Route
    {
        Fixed, Patrol
    }
    Route route;
    Vector2 home, aim;
    float patrolWidth, patrolPeriod;
    float swing, swingRate;
    float cycle, offTime, offset;
    float age, length, width, armAt = 1.5f;
    PulseLaser beam;
    Transform model;
    bool leaving, open;
    SpriteRenderer glow;
    float facing;

    public static ThunderLaserBot Deploy(Vector3 from, Vector2 home, Vector2 aim, float length, float width)
    {
        var go = new GameObject("Indestructible laser drone");
        go.transform.position = new Vector3(from.x, from.y, -.4f);
        var bot = go.AddComponent<ThunderLaserBot>();
        bot.home = home;
        bot.aim = aim.normalized;
        bot.length = length;
        bot.width = width;
        bot.facing = Vector2.SignedAngle(Vector2.down, bot.aim);
        var model = new GameObject("Laser drone");
        model.transform.SetParent(go.transform, false);
        model.transform.localScale = Vector3.one * .5f;
        model.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.CombatHull(9);
        var renderer = model.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = CombatMaterials.Fleet;
        renderer.sortingLayerName = "Enemy";
        bot.model = model.transform;
        bot.glow = ItemSpark.Sprite(go.transform, "Drone core", "standard", LevelProjectilePalette.Accent(LevelRun.Current), .45f);
        bot.glow.transform.localPosition = new Vector3(0, 0, -.2f);
        return bot;
    }

    // Swing the beam smoothly through an arc, reversing at each end.
    public ThunderLaserBot Swing(float arc, float degreesPerSecond)
    {
        swing = arc;
        swingRate = degreesPerSecond;
        return this;
    }

    // Travel right across the arena and back, as a group sharing one clock.
    public ThunderLaserBot Patrol(float width, float period)
    {
        route = Route.Patrol;
        patrolWidth = width;
        patrolPeriod = period;
        return this;
    }

    // Every drone of a formation first fires at the same moment, so swings stay in step.
    public ThunderLaserBot ArmAt(float seconds)
    {
        armAt = seconds;
        return this;
    }

    // Shut down for a window once per cycle, so the player can cross the lane.
    public ThunderLaserBot ShutDown(float cycleSeconds, float offSeconds, float offsetSeconds)
    {
        cycle = cycleSeconds;
        offTime = offSeconds;
        offset = offsetSeconds;
        return this;
    }

    Vector2 Station()
    {
        if (route == Route.Fixed)
            return home;
        float t = Mathf.PingPong(Mathf.Max(0, age - armAt) / Mathf.Max(.1f, patrolPeriod) * 2, 1);
        return home + Vector2.right * Mathf.Lerp(0, patrolWidth, Mathf.SmoothStep(0, 1, t));
    }

    bool Off
    {
        get
        {
            if (cycle <= 0)
                return false;
            float t = Mathf.Repeat(age - offset, cycle);
            return t < offTime;
        }
    }

    void Update()
    {
        if (GameRules.playerDisabled || WorldTimeStop.Frozen)
            return;
        float dt = Time.deltaTime * LevelRun.EnemyTempoScale;
        age += dt;
        glow.transform.localScale = Vector3.one * (.42f + .08f * Mathf.Sin(age * 24));
        if (leaving)
        {
            transform.position += Vector3.up * dt * 9;
            if (transform.position.y > Camera.UpLimit + 2)
                Destroy(gameObject);
            return;
        }
        bool arrived = Vector2.Distance(transform.position, Station()) < .05f;
        Vector2 p = Vector2.MoveTowards(transform.position, Station(), dt * (arrived ? 30 : 9));
        transform.position = new Vector3(p.x, p.y, -.4f);
        if (!arrived)
        {
            Face(facing, dt);
            return;
        }
        bool off = Off;
        if (off && !open)
        {
            open = true;
            if (beam != null && beam.IsAttachedTo(transform))
                beam.Cease();
            beam = null;
        }
        else if (!off && open)
            open = false;
        if (!open && age >= armAt && (beam == null || !beam.IsAttachedTo(transform)))
            Fire();
        // Facing follows the live beam; a shut-down drone points its emitter at the sky.
        float target = open ? 180 : beam != null ? Vector2.SignedAngle(Vector2.down, beam.Heading) : facing;
        Face(target, dt);
    }

    void Face(float angle, float dt)
    {
        float current = model.localEulerAngles.z;
        model.localRotation = Quaternion.Euler(0, 0, Mathf.MoveTowardsAngle(current, angle, dt * 540));
    }

    void Fire()
    {
        beam = CombatRound.Launch<PulseLaser>(transform.position, aim, 0, .2f);
        if (beam == null)
            return;
        beam.Bind(transform, Vector3.zero);
        beam.warning = .85f;
        beam.width = width;
        beam.length = length;
        beam.Sustain();
        if (swing > 0)
        {
            // Restart each swing from the original aim so every drone stays in step.
            beam.sweep = swingRate;
            beam.reversalArc = swing;
        }
    }

    public void Dismiss()
    {
        leaving = true;
        if (beam != null && beam.IsAttachedTo(transform))
            beam.Cease();
    }

    void OnDestroy()
    {
        if (beam != null && beam.IsAttachedTo(transform))
            beam.Cease();
    }
}
