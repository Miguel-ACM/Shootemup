using UnityEngine;

// An indestructible laser drone deployed by the Thunder boss. It holds a station
// and keeps one charged attached beam. The boss can power it down and move it: the
// beam ceases and the drone turns its emitter to the sky while it travels, then
// turns back and recharges with a visible warning before firing again.
// It is not an enemy: player fire passes through it and it never collides.
public sealed class ThunderLaserBot : MonoBehaviour
{
    Vector2 home, aim;
    float swing, swingRate;
    public bool Powered { get; set; } = true;
    float age, length, width, armAt = 1.5f;
    PulseLaser beam;
    Transform model;
    bool leaving, open;
    SpriteRenderer glow, tip;
    LineRenderer coil;
    float facing, nextSpark;

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
        model.transform.localScale = Vector3.one * .62f;
        model.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.ThunderDrone();
        var renderer = model.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = CombatMaterials.Fleet;
        renderer.sortingLayerName = "Enemy";
        bot.model = model.transform;
        var accent = LevelProjectilePalette.Accent(LevelRun.Current);
        bot.glow = ItemSpark.Sprite(model.transform, "Capacitor dome", "standard", accent, .32f);
        bot.glow.transform.localPosition = new Vector3(0, .06f, -.42f);
        bot.tip = ItemSpark.Sprite(model.transform, "Emitter lens", "standard", accent, .3f);
        bot.tip.transform.localPosition = new Vector3(0, -.95f, -.25f);
        // A charge ring round the capacitor that tightens and brightens as the beam charges.
        bot.coil = CombatVisuals.Line(go.transform, accent, .04f);
        return bot;
    }

    // Swing the beam smoothly through an arc, reversing at each end.
    public ThunderLaserBot Swing(float arc, float degreesPerSecond)
    {
        swing = arc;
        swingRate = degreesPerSecond;
        return this;
    }

    // Every drone of a formation first fires at the same moment, so swings stay in step.
    public ThunderLaserBot ArmAt(float seconds)
    {
        armAt = seconds;
        return this;
    }

    public void MoveTo(Vector2 station)
    {
        home = station;
    }

    void Update()
    {
        if (GameRules.playerDisabled || WorldTimeStop.Frozen)
            return;
        float dt = Time.deltaTime * LevelRun.EnemyTempoScale;
        age += dt;
        bool live = beam != null && beam.IsAttachedTo(transform);
        float charge = live ? beam.Charge : 0, power = live ? beam.Power : 0;
        float flicker = .5f + .5f * Mathf.Sin(age * 31) * Mathf.Sin(age * 17);
        glow.transform.localScale = Vector3.one * (.26f + .08f * flicker + .1f * charge);
        tip.transform.localScale = Vector3.one * (open ? .12f : .2f + .25f * charge + .15f * power * flicker);
        var accent = LevelProjectilePalette.Accent(LevelRun.Current);
        coil.enabled = !open;
        if (coil.enabled)
        {
            var c = Color.Lerp(accent * .6f, Color.white, charge * (1 - power) * .6f);
            c.a = .35f + .5f * charge;
            coil.startColor = coil.endColor = c;
            CombatVisuals.Ring(coil, transform.position, Mathf.Lerp(.85f, .5f, charge) + .04f * flicker);
        }
        // Sparks crawl off the emitter while it charges.
        if (live && charge < 1 && age > nextSpark)
        {
            nextSpark = age + .12f;
            PSXImpactEffects.Vanish(tip.transform.position, Color.Lerp(accent, Color.white, .5f), .25f);
        }
        if (leaving)
        {
            transform.position += Vector3.up * dt * 9;
            if (transform.position.y > Camera.UpLimit + 2)
                Destroy(gameObject);
            return;
        }
        bool arrived = Vector2.Distance(transform.position, home) < .05f;
        Vector2 p = Vector2.MoveTowards(transform.position, home, dt * (arrived ? 30 : 7));
        transform.position = new Vector3(p.x, p.y, -.4f);
        bool off = !Powered || !arrived;
        if (off && !open)
        {
            open = true;
            if (beam != null && beam.IsAttachedTo(transform))
                beam.Cease();
            beam = null;
        }
        else if (!off && open)
            open = false;
        if (!open && arrived && age >= armAt && (beam == null || !beam.IsAttachedTo(transform)))
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
