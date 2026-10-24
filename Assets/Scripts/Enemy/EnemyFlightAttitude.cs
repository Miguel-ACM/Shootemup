using UnityEngine;

// Shared visual steering for both legacy models and the district fleet.
// Only the model rotates: movement paths, colliders and shot directions stay authored.
public sealed class EnemyFlightAttitude
{
    Vector3 previous;
    Vector2 velocity;
    float heading, bank, pitch;
    bool initialized;

    public Quaternion Step(Vector3 position, float targetHeading, float weight, float dt)
    {
        if (!initialized)
        {
            initialized = true;
            previous = position;
            heading = targetHeading;
        }
        if (dt <= 0) return Rotation;
        Vector2 measured = (position - previous) / dt;
        previous = position;
        // Spawn placement and teleports are not flight maneuvers.
        if (measured.sqrMagnitude > 1600) { velocity = Vector2.zero; return Rotation; }
        float response = Mathf.Lerp(.11f, .26f, weight);
        float blend = 1 - Mathf.Exp(-dt / response);
        Vector2 nextVelocity = Vector2.Lerp(velocity, measured, blend);
        Vector2 acceleration = (nextVelocity - velocity) / dt;
        velocity = nextVelocity;
        float turn = Mathf.DeltaAngle(heading, targetHeading);
        float yaw = turn * (1 - Mathf.Exp(-dt / Mathf.Lerp(.16f, .32f, weight)));
        heading += yaw;
        Vector2 right = Quaternion.Euler(0, 0, heading) * Vector2.right;
        Vector2 forward = Quaternion.Euler(0, 0, heading) * Vector2.down;
        float limit = Mathf.Lerp(32, 16, weight);
        float desiredBank = Mathf.Clamp(Vector2.Dot(velocity, right) * 4.5f
            + Vector2.Dot(acceleration, right) * .38f + yaw / dt * .13f, -limit, limit);
        float desiredPitch = Mathf.Clamp(-Vector2.Dot(acceleration, forward) * .5f, -9, 9);
        bank = Mathf.Lerp(bank, desiredBank, blend);
        pitch = Mathf.Lerp(pitch, desiredPitch, blend);
        return Rotation;
    }

    Quaternion Rotation => Quaternion.AngleAxis(heading, Vector3.forward) * Quaternion.Euler(pitch, bank, 0);
}
