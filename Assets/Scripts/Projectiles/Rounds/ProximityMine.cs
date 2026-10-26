using UnityEngine;
public sealed class ProximityMine : CombatRound
{
    LineRenderer radius;
    protected override void Initialize()
    {
        if (radius == null)
            radius = CombatVisuals.Line(transform, Accent, .025f);
        var color = Accent;
        color.a = .4f;
        radius.startColor = radius.endColor = color;
        visual.color = Color.gray;
        collision.enabled = false;
        lifetime = 6;
    }

    protected override void Tick(float dt)
    {
        if (age < 1)
            transform.position += (Vector3)heading * speed * (1 - age) * dt;
        CombatVisuals.Ring(radius, transform.position, 1.1f);
        var player = CombatVisuals.Player;
        float distance = player != null ? Vector2.Distance(player.position, transform.position) : 9;
        // The mine cannot arm beneath a player; retreating gives it an arming opportunity.
        bool armed = age >= 1.4f && (collision.enabled || distance > 1.5f);
        collision.enabled = armed;
        visual.color = armed ? Color.Lerp(new Color(.65f, .65f, .65f), Color.white, .65f + .35f * Mathf.Sin(age * 8)) : Color.gray;
        if (armed && (age > 4.5f || distance < 1.1f) && Ring(Difficulty.Easy ? 6 : 10, 2.6f, .18f))
            Release();
    }
}
