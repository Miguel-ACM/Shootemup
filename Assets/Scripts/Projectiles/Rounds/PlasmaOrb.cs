using UnityEngine;
public sealed class PlasmaOrb : CombatRound
{
    float health;
    LineRenderer outline;
    protected override void Initialize()
    {
        // Player rounds can hit this shell; other hostile rounds and enemy hulls cannot.
        gameObject.layer = ProjectileCollisionLayers.Destructible;
        health = 12;
        visual.color = Color.white;
        if (outline == null)
            outline = CombatVisuals.Line(transform, Accent, .035f);
        outline.startColor = outline.endColor = Accent;
        CombatVisuals.Ring(outline, transform.position, transform.localScale.x * .6f);
    }

    protected override void Tick(float dt)
    {
        base.Tick(dt);
        CombatVisuals.Ring(outline, transform.position, transform.localScale.x * .6f);
    }

    public void Hit(float amount)
    {
        health -= amount;
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
