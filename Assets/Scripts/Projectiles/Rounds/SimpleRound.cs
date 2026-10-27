using UnityEngine;

// The approved circular sprite, existing pool and exact pixel contact remain
// shared. Motion and delivery are reset on every launch; no child components.
public sealed class SimpleRound : CombatRound
{
    enum Motion { Straight, Curl, Orbit }
    enum Delivery { None, Trail, Seed }
    Motion motion;
    Delivery delivery;
    float turn, turnUntil, nextRelease, releaseUntil, releaseEvery, seedAt;
    float orbitRadius, orbitPhase, orbitRate, grow, satelliteRadius, satellitePhase, satelliteRate;
    int seedCount, seedGap;
    float seedRadius, seedRate, seedDrift, seedPhase;
    Vector2 center, lastOrbitPosition;
    public bool Orbiting => motion == Motion.Orbit;
    public bool Trailing => delivery == Delivery.Trail;
    public bool ChargingSeed => delivery == Delivery.Seed;
    public float OrbitRadius => orbitRadius + satelliteRadius;
    protected override float ContactSpeed => speed + (motion == Motion.Orbit ?
        Mathf.Abs(orbitRate) * Mathf.Deg2Rad * orbitRadius + Mathf.Abs(satelliteRate) * Mathf.Deg2Rad * satelliteRadius + OrbitRadius * 2 / Mathf.Max(.1f, grow) : 0);

    protected override void Initialize()
    {
        motion = Motion.Straight;
        delivery = Delivery.None;
        turn = turnUntil = nextRelease = releaseUntil = releaseEvery = seedAt = 0;
        orbitRadius = orbitPhase = orbitRate = satelliteRadius = satellitePhase = satelliteRate = 0;
        seedCount = seedGap = 0;
        seedRadius = seedRate = seedDrift = seedPhase = 0;
        grow = 1;
        center = lastOrbitPosition = transform.position;
    }
    public SimpleRound Curl(float degreesPerSecond, float seconds = 3)
    {
        motion = Motion.Curl; turn = degreesPerSecond; turnUntil = seconds; return this;
    }
    public SimpleRound Orbit(float radius, float phase, float degreesPerSecond, float seconds = 13,
        float innerRadius = 0, float innerPhase = 0, float innerDegrees = 0, float growth = 1.35f)
    {
        motion = Motion.Orbit; orbitRadius = radius; orbitPhase = phase; orbitRate = degreesPerSecond;
        satelliteRadius = innerRadius; satellitePhase = innerPhase; satelliteRate = innerDegrees;
        grow = growth; lifetime = seconds; center = lastOrbitPosition = transform.position; return this;
    }
    public SimpleRound LeaveTrail(float every = .28f, float seconds = 5)
    {
        delivery = Delivery.Trail; nextRelease = .65f; releaseEvery = every; releaseUntil = seconds;
        lifetime = Mathf.Max(lifetime, seconds + 2); return this;
    }
    public SimpleRound IceSeed(float delay, int beads, int gap, float radius, float rotation, float drift, float phase)
    {
        delivery = Delivery.Seed; seedAt = delay; seedCount = beads; seedGap = gap;
        seedRadius = radius; seedRate = rotation; seedDrift = drift; seedPhase = phase;
        lifetime = delay + 8; return this;
    }
    public SimpleRound Lasts(float seconds) { lifetime = seconds; return this; }

    public static Vector2 OrbitOffset(float t, float radius, float phase, float rate, float innerRadius, float innerPhase, float innerRate, float growth)
    {
        float expansion = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / Mathf.Max(.01f, growth)));
        return (CombatVisuals.Rotate(Vector2.down, phase + rate * t) * radius +
            CombatVisuals.Rotate(Vector2.down, innerPhase + innerRate * t) * innerRadius) * expansion;
    }
    protected override void Tick(float dt)
    {
        if (motion == Motion.Orbit)
        {
            // Retain displacement from clearing/support abilities such as Pocket Wind.
            center += (Vector2)transform.position - lastOrbitPosition;
            center += heading * speed * dt;
            Vector2 p = center + OrbitOffset(age, orbitRadius, orbitPhase, orbitRate, satelliteRadius, satellitePhase, satelliteRate, grow);
            lastOrbitPosition = p;
            transform.position = new Vector3(p.x, p.y, -.5f);
        }
        else
        {
            if (motion == Motion.Curl && age < turnUntil) heading = CombatVisuals.Rotate(heading, turn * dt);
            float brake = delivery == Delivery.Seed ? Mathf.Clamp01((seedAt - age) / .75f) : 1;
            transform.position += (Vector3)heading * speed * brake * dt;
        }
        Orient();
        if (delivery == Delivery.Trail && age >= nextRelease && age <= releaseUntil && Live.Count + 2 <= Limit)
        {
            nextRelease = age + releaseEvery;
            Vector2 side = new Vector2(-heading.y, heading.x);
            float edge = transform.localScale.x * .5f + .035f;
            for (int sign = -1; sign <= 1; sign += 2)
            {
                var bead = Launch<SimpleRound>(transform.position + (Vector3)side * sign * edge,
                    (side * sign + heading * .2f).normalized, 1.3f, .14f);
                if (bead != null) { bead.generation = 1; bead.Lasts(8); }
            }
        }
        else if (delivery == Delivery.Seed)
        {
            float charge = Mathf.Clamp01((age - seedAt + .8f) / .8f);
            visual.color = Color.white * (1 + charge * (.12f + .08f * Mathf.Sin(age * 18)));
            if (age < seedAt || Live.Count + seedCount - seedGap > Limit) return;
            // A visible, braking shell delivers a complete annulus with an authored opening.
            for (int i = seedGap; i < seedCount; i++)
            {
                var bead = Launch<SimpleRound>(transform.position, Vector2.down, seedDrift, .18f);
                if (bead != null) bead.Orbit(seedRadius, seedPhase + i * 360f / seedCount, seedRate);
            }
            Release();
        }
    }
}
