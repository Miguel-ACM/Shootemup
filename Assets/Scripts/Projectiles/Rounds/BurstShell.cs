using UnityEngine;
public sealed class BurstShell : CombatRound
{
    static ParticleSystem ignition;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetIgnition()
    {
        ignition = null;
    }

    float nextSpark;
    float cruise, brake;
    public float TravelDistance
    {
        get; private set;
    }

    protected override void Initialize()
    {
        visual.color = Color.white;
        nextSpark = 0;
        SetTravelDistance(3.8f);
    }

    public BurstShell SetTravelDistance(float distance)
    {
        TravelDistance = Mathf.Max(.1f, distance);
        float brakingDistance = Mathf.Min(TravelDistance * .35f, 1.5f);
        float velocity = Mathf.Max(.1f, speed);
        cruise = (TravelDistance - brakingDistance) / velocity;
        brake = 2 * brakingDistance / velocity;
        lifetime = Mathf.Max(9, cruise + brake + 3);
        return this;
    }

    float DistanceAt(float t)
    {
        float coast = Mathf.Min(t, cruise), slow = Mathf.Clamp(t - cruise, 0, brake);
        return speed * (coast + slow - slow * slow / (2 * brake));
    }

    void Spark(float charge)
    {
        if (ignition == null)
        {
            ignition = PSXImpactEffects.Create(null, "Shared shell ignition sparks", 384);
            ignition.GetComponent<ParticleSystemRenderer>().sortingOrder = 14;
        }
        float shellRadius = transform.localScale.x * .5f;
        float radius = Mathf.Lerp(shellRadius * 2.3f + .2f, shellRadius * 1.1f + .025f, Mathf.SmoothStep(0, 1, charge));
        for (int i = 0; i < 3; i++)
        {
            float angle = Random.value * Mathf.PI * 2;
            var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
            var particle = new ParticleSystem.EmitParams();
            float life = Mathf.Lerp(.25f, .14f, charge) * Random.Range(.85f, 1.1f);
            particle.position = transform.position + direction * radius + Vector3.back * .12f;
            particle.velocity = -direction * (radius - shellRadius * .72f) / life;
            particle.startColor = Color.Lerp(Accent, Color.white, charge * .30f);
            float size = Random.Range(.045f, .075f) * Mathf.Lerp(.8f, 1.2f, charge);
            particle.startSize3D = new Vector3(size, size * 1.8f, size);
            particle.rotation3D = new Vector3(0, 0, 90 - angle * Mathf.Rad2Deg);
            particle.startLifetime = life;
            ignition.Emit(particle, 1);
        }
    }

    protected override void Tick(float dt)
    {
        transform.position += (Vector3)heading * (DistanceAt(age) - DistanceAt(Mathf.Max(0, age - dt)));
        float stop = cruise + brake;
        float charge = Mathf.Clamp01((age - stop) / .8f);
        visual.color = Color.white;
        if (age >= stop && age >= nextSpark)
        {
            Spark(charge);
            nextSpark = age + Mathf.Lerp(.10f, .05f, charge);
        }
        if (age >= stop + .85f && Burst())
            Release();
    }

    // Plains and Space shells explode in a full 360-degree ring (Miguel).
    // Snow shells throw their shards straight up and out of sight, then the same
    // number of flakes drift down from the top of the screen, spread across the
    // shell's column. Elsewhere a shell throws a forward cone of fast and slow shrapnel.
    bool Burst()
    {
        var district = LevelRun.Current;
        float facing = Vector2.SignedAngle(Vector2.down, heading);
        if (district == LevelId.Space || district == LevelId.Fields)
            return Ring(12, 2.9f, .18f, facing);
        if (district == LevelId.Snow)
            return Snowfall(12);
        const int count = 9;
        if (generation > 0 || Live.Count + count > Limit)
            return false;
        for (int i = 0; i < count; i++)
        {
            float lane = i - (count - 1) * .5f;
            var shard = Launch<SimpleRound>(transform.position, CombatVisuals.Rotate(heading, lane * 200f / (count - 1)), i % 2 == 0 ? 3.3f : 2.1f, .18f);
            if (shard != null)
                shard.generation = 1;
        }
        return true;
    }

    bool Snowfall(int flakes)
    {
        if (generation > 0 || Live.Count + flakes * 2 > Limit)
            return false;
        for (int i = 0; i < flakes; i++)
        {
            float lane = i - (flakes - 1) * .5f;
            // The upward throw is the animation: fast, short-lived and gone off the top.
            var spray = Launch<SimpleRound>(transform.position, CombatVisuals.Rotate(Vector2.up, lane * 6), 9 + (i % 3), .16f);
            spray.FadeAt(.45f);
            float x = Mathf.Clamp(transform.position.x + lane * .55f, Camera.LeftLimit + .3f, Camera.RightLimit - .3f);
            var flake = Launch<SimpleRound>(new Vector3(x, Camera.UpLimit + .4f), Vector2.down, 1.4f + (i % 3) * .25f, .19f);
            flake.Pause(.05f, .5f + (i % 4) * .12f, Turn.Down, lane * 2, 1).Sway(i % 2 == 0 ? .35f : -.35f, .45f);
            if (spray != null)
                spray.generation = 2;
            if (flake != null)
                flake.generation = 1;
        }
        return true;
    }
}
