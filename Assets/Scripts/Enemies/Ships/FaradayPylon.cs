using UnityEngine;

// Faraday Pylon: a double-ended Tesla pylon that rides a side wall, with a mirrored twin on
// the other wall. On each accent both charge and fire a horizontal bar inward. The bars stop
// short of each other and leave a door, and the pair keeps sliding along the walls, so the
// bars sweep through the arena and the pilot has to pass through the door. The door steps to
// a new place on every accent and is narrower on harder difficulties. Destroying either
// pylon opens its half of the bar.
[UnityEngine.Scripting.Preserve]
public sealed class FaradayPylon : DistrictEnemy
{
    // Door centres, as fractions of the half-width, stepping a few units on each accent.
    static readonly float[] Doors = { -.35f, .1f, .5f, .15f, -.25f, -.6f };
    FaradayPylon twin;
    bool follower;
    int doorPhase, accents;
    PulseLaser bar;
    SpriteRenderer glow;

    float Inward => transform.position.x < 0 ? 1 : -1;
    protected override Vector2 CueAxis => new Vector2(Inward, 0);
    static float DoorWidth => Difficulty.Easy ? 2.4f : Difficulty.Hard ? 1.7f : 2.0f;

    protected override void Configure()
    {
        hullScale = .55f;
        health = 40;
        attackInterval = 3.2f;
        nextAttack = 1.4f;
        Mover<DriftDownPattern>();
    }

    public override void FlyPhrase(WaveUnit unit)
    {
        base.FlyPhrase(unit);
        // Each rung of a ladder, and each side, starts on its own door.
        doorPhase = Mathf.RoundToInt(unit.firePhase * 1.45f) + (unit.side < 0 ? 3 : 0);
    }

    protected override void Start()
    {
        base.Start();
        glow = ItemSpark.Sprite(Hull, "Capacitor glow", "standard", LevelProjectilePalette.Accent(LevelRun.Current), .3f);
        glow.transform.localPosition = new Vector3(0, .06f, -.42f);
        if (follower)
        {
            nextAttack = float.PositiveInfinity;
            return;
        }
        // The twin mirrors this pylon on the opposite wall and fires only on its accents.
        twin = (FaradayPylon)Spawn(typeof(FaradayPylon), new Vector3(-transform.position.x, transform.position.y, transform.position.z));
        twin.follower = true;
        twin.twin = this;
        twin.doorPhase = doorPhase;
        ShareEncounterWith(twin);
        var mirror = twin.gameObject.AddComponent<PylonMirror>();
        mirror.Leader = transform;
        twin.UseMovement(mirror);
    }

    // The leader calls the accent for both pylons; the door is shared.
    protected override void Attack()
    {
        float door = Doors[(doorPhase + accents++) % Doors.Length] * Camera.RightLimit;
        FireBar(door);
        if (twin != null)
            twin.FireBar(door);
    }

    // A charged horizontal bar from the inward emitter, ending at the near edge of the door.
    void FireBar(float door)
    {
        var local = InwardPort;
        var origin = Hull.TransformPoint(local);
        float edge = door - Inward * DoorWidth * .5f;
        var beam = Fire<PulseLaser>(origin, new Vector2(Inward, 0), 0);
        if (beam == null)
            return;
        beam.Bind(Hull, local);
        beam.warning = Difficulty.Easy ? 1.25f : 1.05f;
        beam.activeTime = 1.3f;
        beam.width = .2f;
        beam.length = Mathf.Max(.5f, Mathf.Abs(edge - origin.x));
        bar = beam;
    }

    Vector3 InwardPort => EnemyWeaponLayout.Ports(hull)[Inward > 0 ? 1 : 0];

    // Both pylons show the coming accent at their inward emitters.
    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        if (follower)
            return;
        firingCue.ClearWarning();
        Cue(Hull.TransformPoint(InwardPort), seconds);
        if (twin != null && twin.Hull != null)
            twin.Cue(twin.Hull.TransformPoint(twin.InwardPort), seconds);
    }

    // The capacitor dome glows up while a bar charges and flickers while it fires.
    void LateUpdate()
    {
        if (glow == null)
            return;
        bool live = bar != null && bar.IsAttachedTo(Hull);
        float flicker = .5f + .5f * Mathf.Sin(Time.time * 31) * Mathf.Sin(Time.time * 17);
        float charge = live ? bar.Charge : 0, power = live ? bar.Power : 0;
        glow.transform.localScale = Vector3.one * (.24f + .08f * flicker + .16f * charge + .1f * power * flicker);
    }
}

// Keeps a follower pylon on the opposite wall, level with its leader. Without a leader it
// carries on the way the pair was travelling and leaves the arena.
public sealed class PylonMirror : PatternMover
{
    public Transform Leader;
    Vector3 travel;
    protected override void StepPattern(float dt)
    {
        if (Leader == null)
        {
            SetFiring(false);
            transform.position += travel * dt;
            if (transform.position.y > Camera.UpLimit + 1.5f || transform.position.y < Camera.DownLimit - 1.5f)
                GetComponent<AEnemy>().Depart();
            return;
        }
        var target = new Vector3(-Leader.position.x, Leader.position.y, transform.position.z);
        if (dt > 0)
            travel = (target - transform.position) / dt;
        transform.position = target;
        SetFiring(true);
    }
}

public static partial class WavePartLibrary
{
    static readonly FlightCurve FaradayDescent = Route("Thunder / Faraday descent", Vector2.right, -0.93f, 1.18f, -0.93f, 0.9f, -0.93f, 0.62f, -0.93f, 0.34f, -0.93f, 0.06f, -0.93f, -0.22f);
    static readonly FlightCurve FaradayClimb = Route("Thunder / Faraday climb", Vector2.right, -0.93f, -0.22f, -0.93f, 0.06f, -0.93f, 0.34f, -0.93f, 0.62f, -0.93f, 0.9f, -0.93f, 1.18f);
    // A single gate sweeps down the arena; later, a ladder of gates climbs up it.
    public static readonly WavePart[] FaradayPylonParts =
    {
        new WavePart("Faraday gate", nameof(FaradayPylon), FaradayDescent, WaveShape.Solo, 1, 8.0f, 3, new[] { 0.25f, 0.58f }),
        new WavePart("Faraday ladder", nameof(FaradayPylon), FaradayClimb, WaveShape.Train, 2, 8.5f, 5, new[] { 0.3f, 0.62f }),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedFaradayPylon;

    // A double-ended Tesla pylon: the boss drones' capacitor body and dark dome on a column
    // of insulator discs, with a coiled emitter on each flank, so either flank can face the
    // arena from its wall.
    public static Mesh FaradayPylonMesh()
    {
        if (cachedFaradayPylon != null)
            return cachedFaradayPylon;
        var b = new Builder();
        b.Drum(Vector3.zero, .42f, .34f, 13, 8, 6);
        b.Drum(new Vector3(0, 0, -.2f), .3f, .08f, 1, 8);
        b.Ball(new Vector3(0, .06f, -.3f), new Vector3(.26f, .26f, .16f), 9);
        foreach (int s in new[] { -1, 1 })
        {
            // Insulator stack and cap above and below the body.
            for (int i = 0; i < 3; i++)
                b.Drum(new Vector3(0, s * (.5f + i * .16f), -.05f), .2f - i * .03f, .08f, 10, 8);
            b.Ball(new Vector3(0, s * .98f, -.1f), new Vector3(.2f, .2f, .14f), 9);
            // Coiled emitter on each flank.
            b.Box(new Vector3(s * .62f, 0, -.1f), new Vector3(.56f, .15f, .15f), 11);
            for (int i = 0; i < 3; i++)
                b.Drum(new Vector3(s * (.47f + i * .15f), 0, -.12f), .15f, .09f, 10, 8);
            b.Drum(new Vector3(s * .93f, 0, -.12f), .11f, .1f, 9, 8);
            // Lightning-bolt fins between the column and the emitters.
            b.Prism(new[] { V(s * .26f, .3f), V(s * .62f, .62f), V(s * .46f, .36f), V(s * .72f, .3f), V(s * .4f, .16f) }, .08f, 6, new Vector3(0, 0, -.05f), 1);
            b.Prism(new[] { V(s * .4f, -.16f), V(s * .72f, -.3f), V(s * .46f, -.36f), V(s * .62f, -.62f), V(s * .26f, -.3f) }, .08f, 6, new Vector3(0, 0, -.05f), 1);
        }
        return cachedFaradayPylon = b.Finish("Faraday pylon double-ended emitter");
    }
}
