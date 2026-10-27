using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class TaffyDuelist : DistrictMiniboss
{
    protected override void Configure()
    {
        hull = 57;
        hullScale = .9f;
        health = 480;
        attackInterval = (0.4f);
        nextAttack = 1.1f;
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        int beat = volley % 10;
        if (beat != 3 && beat != 4 && beat != 9)
            Cue(Muzzle(beat % 2), seconds);
    }

    protected override void Attack()
    {
        int beat = (volley - 1) % 10;
        if (beat == 3 || beat == 4 || beat == 9)
            return;
        var port = Hull.TransformPoint(new Vector3((beat % 2 == 0 ? -1 : 1) * .74f, -.55f, -.25f));
        for (int i = 0; i < (3); i++)
            Fire<NeedleRound>(port, CombatVisuals.Rotate(CombatVisuals.Aim(port), (i - ((3) - 1) * .5f) * (Enraged ? 22 : 26)), 4.8f, .19f);
        if (beat == 6)
            for (int i = 0; i < (10); i++)
                Fire<SimpleRound>(port, CombatVisuals.Rotate(Vector2.down, i * 360f / (10) + 18), 3.3f, .16f);
    }
}
