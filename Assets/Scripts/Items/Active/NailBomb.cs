using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class NailBomb : OffensiveActiveItem
{
    void Awake()
    {
        SetData("Nail Bomb", "Pull the pin", "Throw. Press again to burst piercing nails. 25 energy.", ItemType.active);
        cost = 25; cd = 4;
    }
    protected override OffensiveCast Launch() => CreateCast<NailCanister>("Nail canister");
    protected override bool Repress()
    {
        if (Cast is not NailCanister bomb || bomb.Age < .12f) return false;
        bomb.Detonate();
        return true;
    }
}

public sealed class NailCanister : OffensiveCast
{
    Transform model;
    bool detonated;
    protected override void Begin()
    {
        transform.position += new Vector3(0, .65f, -1);
        model = Model(nameof(NailBomb), .6f);
        ItemSound.Cue("shotShell", volume: .18f, pitch: .8f, cooldown: 0);
    }
    protected override void Step(float dt)
    {
        transform.position += Vector3.up * (8 * Mathf.Clamp01(1 - Age / .9f) * dt);
        model.Rotate(0, 0, dt * 150);
        if (Age >= 3 || transform.position.y >= Camera.UpLimit - .2f) Detonate();
    }
    public void Detonate()
    {
        if (detonated) return;
        detonated = true;
        // Even ring: deliberate gaps and one release, reusing the approved sharp round.
        for (int i = 0; i < 18; i++)
        {
            var nail = Shot(transform.position, CombatVisuals.Rotate(Vector2.up, i * 20), 2.6f, 13, PlayerDelivery.Needle);
            if (nail != null) { nail.pierces = 3; nail.LockHeading(); }
        }
        PSXFireballs.Shockwave(transform.position, 1.1f, .3f, new Color(.82f, .64f, .38f), 2, .18f);
        ItemSound.Cue("itemDepthBurst", volume: .25f, pitch: .85f, cooldown: 0);
        Finish();
    }
}
