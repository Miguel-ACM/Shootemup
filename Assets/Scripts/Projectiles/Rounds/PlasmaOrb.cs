using UnityEngine;
public sealed class PlasmaOrb : CombatRound
{
    // Destructible plasma is sturdy: it takes a sustained stream to burst it.
    // Its outline is a life ring that shrinks to an arc as it loses health.
    const float Sturdiness = 60;
    float health;
    LineRenderer outline;
    public static PlasmaOrb FirstOnBeam(Vector2 start, Vector2 end, float radius, out Vector2 point)
    {
        PlasmaOrb nearest = null;
        point = end;
        float closest = (end - start).sqrMagnitude;
        foreach (var round in Live)
            if (round is PlasmaOrb orb && !orb.ended && BeamContact.Touches(orb.collision, start, end, radius, out var hit))
            {
                float distance = (hit - start).sqrMagnitude;
                if (distance < closest)
                {
                    closest = distance;
                    point = hit;
                    nearest = orb;
                }
            }
        return nearest;
    }
    protected override void Initialize()
    {
        // Player rounds can hit this shell; other hostile rounds and enemy hulls cannot.
        gameObject.layer = ProjectileCollisionLayers.Destructible;
        health = Sturdiness;
        visual.color = Color.white;
        if (outline == null)
            outline = CombatVisuals.Line(transform, Accent, .05f);
        DrawLife();
    }

    protected override void Tick(float dt)
    {
        base.Tick(dt);
        visual.color = Color.Lerp(visual.color, Color.white, dt * 10);
        DrawLife();
    }

    void DrawLife()
    {
        float life = Mathf.Clamp01(health / Sturdiness);
        var color = Color.Lerp(Color.white, Accent, life);
        outline.startColor = outline.endColor = color;
        const int segments = 32;
        int points = Mathf.Max(2, Mathf.CeilToInt(segments * life) + 1);
        float radius = transform.localScale.x * .62f;
        outline.positionCount = points;
        for (int i = 0; i < points; i++)
        {
            float angle = Mathf.PI * .5f - i / (points - 1f) * life * Mathf.PI * 2;
            outline.SetPosition(i, transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), -.15f) * radius);
        }
    }

    public void Hit(float amount)
    {
        if (ended)
            return;
        health -= amount;
        visual.color = Color.white * 1.6f;
        if (health <= 0)
            Vanish();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var shot = other.GetComponent<ABullet>();
        if (shot == null)
            return;
        Hit(shot.damage);
        Destroy(shot.gameObject);
    }
}
