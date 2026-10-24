using UnityEngine;

public class TopTracker : PatternMover
{
    [SerializeField, Min(0)] private float topOffset = 2f;
    [SerializeField, Min(.01f)] private float trackingDuration = 2f;
    [SerializeField, Min(0)] private float lockWarning = .6f;
    [SerializeField, Min(.01f)] private float firingDuration = 1.5f;
    [SerializeField, Min(0)] private float recoveryDuration = .8f;
    [SerializeField, Min(.1f)] private float acceleration = 10f;
    [SerializeField, Min(.1f)] private float deceleration = 14f;
    private Vector2 velocity;
    public Vector2 Velocity => velocity;
    private enum Phase { Enter, Track, Lock, Fire, Recover }
    private Phase phase;
    private float clock;

    protected override void InitializePattern()
    {
        // Small interceptors can correct quickly; loaded gunships carry momentum.
        var enemy = GetComponent<DistrictEnemy>();
        if (enemy == null) return;
        float weight = Mathf.InverseLerp(.45f, 1.1f, enemy.hullScale);
        moveSpeed = Mathf.Lerp(4.8f, 2.8f, weight);
        acceleration = Mathf.Lerp(18f, 5.5f, weight);
        deceleration = Mathf.Lerp(22f, 7f, weight);
    }

    private bool Steer(Vector3 target, float dt, bool approachingLock = false)
    {
        Destination = target;
        Vector2 offset = target - transform.position;
        float distance = offset.magnitude;
        // Braking distance limits the approach; the final proportional stretch
        // avoids crossing the destination and snapping back on the next frame.
        float speed = Mathf.Min(moveSpeed, Mathf.Sqrt(2 * deceleration * distance), distance * 5);
        if (approachingLock) speed = Mathf.Min(speed, deceleration * Mathf.Max(0, trackingDuration - clock));
        Integrate(distance > .001f ? offset / distance * speed : Vector2.zero, dt);
        return distance < .025f && velocity.sqrMagnitude < .0025f;
    }

    private void Integrate(Vector2 desired, float dt)
    {
        bool braking = Vector2.Dot(velocity, desired) < 0 || desired.sqrMagnitude < velocity.sqrMagnitude;
        Vector2 next = Vector2.MoveTowards(velocity, desired, (braking ? deceleration : acceleration) * dt);
        transform.position += (Vector3)((velocity + next) * (.5f * dt));
        velocity = next;
    }

    protected override void StepPattern(float dt)
    {
        // Stable braking and reversals even when a frame spans a phase boundary.
        while (dt > 0)
        {
            float step = Mathf.Min(dt, 1f / 120);
            Advance(step);
            dt -= step;
        }
    }

    private void Advance(float dt)
    {
        clock += dt;
        switch (phase)
        {
            case Phase.Enter:
                if (Steer(InBounds(new Vector3(origin.x, Camera.UpLimit - topOffset, origin.z)), dt))
                { Arrive(); Next(Phase.Track); }
                break;
            case Phase.Track:
                var target = transform.position;
                if (player != null) target.x = player.position.x;
                Steer(InBounds(target), dt, true);
                if (clock >= trackingDuration)
                {
                    LockAim(player != null ? player.position : transform.position + Vector3.down);
                    Next(Phase.Lock);
                }
                break;
            case Phase.Lock:
                Integrate(Vector2.zero, dt);
                if (clock >= lockWarning) { Next(Phase.Fire); SetFiring(true); }
                break;
            case Phase.Fire:
                Integrate(Vector2.zero, dt);
                if (clock >= firingDuration) { SetFiring(false); Next(Phase.Recover); }
                break;
            case Phase.Recover:
                Integrate(Vector2.zero, dt);
                if (clock >= recoveryDuration) { LockAim(null); Next(Phase.Track); }
                break;
        }
    }

    private void Next(Phase next) { phase = next; clock = 0; }
}
