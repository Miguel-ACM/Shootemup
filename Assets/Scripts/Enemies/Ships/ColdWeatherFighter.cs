using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class ColdWeatherFighter : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 6;
        health = 24;
        hullScale = .62f;
        attackInterval = (1.25f);
        Mover<Slalom>();
    }

    // Three rounds that freeze in place, then crawl back toward the player.
    protected override void Attack()
    {
        int count = Dense(3);
        var aim = Aim;
        for (int i = 0; i < count; i++)
        {
            float lane = i - (count - 1) * .5f;
            Fire<SimpleRound>(Muzzle(i), CombatVisuals.Rotate(aim, lane * 24), 3.8f, .19f)
                .Pause(.5f, .55f, CombatRound.Turn.Aim, lane * 8, .7f);
        }
    }
}
