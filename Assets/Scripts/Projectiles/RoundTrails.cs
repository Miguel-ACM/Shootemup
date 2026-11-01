using UnityEngine;

// Rounds that wander (zigzags, sway, gravity, bounces, homing, curls and orbits)
// leave a short fading trail in their own colour. Every trail is a dot in one shared
// particle system under all hostile rounds: one draw call, nothing per round to pool.
public static class RoundTrails
{
    const float Spacing = .07f, Lifetime = .3f;
    static ParticleSystem system;

    public static void Follow(ref Vector2 from, Vector3 position, Color color, float size)
    {
        Vector2 p = position;
        float distance = Vector2.Distance(from, p);
        if (distance < Spacing)
            return;
        // A jump (a pooled round relaunched, a wall wrap, a displacement ability) starts afresh.
        if (distance > 1.2f)
        {
            from = p;
            return;
        }
        if (system == null)
        {
            system = PSXImpactEffects.Create(null, "Erratic round trails", 4000);
            // Under every hostile round, so the older-above-newer round order is untouched.
            system.GetComponent<ParticleSystemRenderer>().sortingOrder = 14;
        }
        var emit = new ParticleSystem.EmitParams();
        color.a = .55f;
        emit.startColor = color;
        emit.startSize3D = new Vector3(size, size, 1);
        emit.startLifetime = Lifetime;
        int steps = Mathf.Min(4, (int)(distance / Spacing));
        for (int i = 1; i <= steps; i++)
        {
            Vector2 at = Vector2.Lerp(from, p, i / (float)steps);
            emit.position = new Vector3(at.x, at.y, position.z);
            system.Emit(emit, 1);
        }
        from = p;
    }
}
