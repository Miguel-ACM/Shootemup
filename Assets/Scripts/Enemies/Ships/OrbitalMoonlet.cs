using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class OrbitalMoonlet : SpaceSatellite
{
    protected override void Configure()
    {
        hull = 44;
        hullScale = .28f;
        health = 24;
        attackInterval = (1.4f);
        nextAttack = .8f;
    }

    // A binary star: two beads orbiting each other as they travel at the player.
    protected override void Attack()
    {
        var aim = Aim;
        for (int i = 0; i < 2; i++)
        {
            var bead = Fire<SimpleRound>(Muzzle(), aim, 2.6f, .18f);
            if (bead != null)
                bead.Orbit(.4f, i * 180, volley % 2 == 0 ? 220 : -220, 9, 0, 0, 0, .25f);
        }
    }
}
