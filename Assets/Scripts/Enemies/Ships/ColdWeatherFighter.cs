using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class ColdWeatherFighter : DistrictEnemy
{
    protected override void Configure()
    {
        health = 24;
        hullScale = .62f;
        attackInterval = (1.25f);
        Mover<Slalom>();
    }

    // Three chevrons freeze in place, then re-aim and crawl toward the player.
    protected override void Attack()
    {
        int count = Dense(3);
        var aim = Aim;
        for (int i = 0; i < count; i++)
        {
            float lane = i - (count - 1) * .5f;
            Fire<ChevronRound>(Muzzle(i), CombatVisuals.Rotate(aim, lane * 24), 3.8f, .19f)
                .Pause(.5f, .55f, CombatRound.Turn.Aim, lane * 8, .7f);
        }
    }
}
