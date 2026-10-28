using UnityEngine;
public sealed class ScatterCarrier : CombatRound
{
    float nextRelease;
    // Plains carriers and the needles they release share one orange accent (Miguel, 2026-10-25).
    static readonly Color PlainsOrange = new Color(1, .58f, .08f);
    protected override void Initialize()
    {
        nextRelease = Random.Range(.7f, 1.2f);
        Orient();
        if (LevelRun.Current == LevelId.Fields)
            GetComponent<ProjectileSpriteTint>().SetAccent(PlainsOrange);
    }

    void Release(Vector3 at, Vector2 direction)
    {
        var needle = Launch<NeedleRound>(at, direction, Random.Range(2.4f, 3.4f), .11f);
        if (needle != null && LevelRun.Current == LevelId.Fields)
            needle.GetComponent<ProjectileSpriteTint>().SetAccent(PlainsOrange);
    }

    protected override void Tick(float dt)
    {
        base.Tick(dt);
        if (age < nextRelease)
            return;
        // Sparse, irregular side releases: usually one flank, sometimes both, never a metronome.
        nextRelease = age + Random.Range(.85f, 1.7f);
        var side = new Vector2(-heading.y, heading.x);
        float muzzle = transform.localScale.x * .435f;
        var center = transform.position - (Vector3)heading * transform.localScale.y * .022f;
        int flank = Random.value < .5f ? -1 : 1;
        bool both = Random.value < .3f;
        for (int sign = -1; sign <= 1; sign += 2)
            if (both || sign == flank)
                Release(center + (Vector3)side * sign * muzzle, CombatVisuals.Rotate(side * sign, Random.Range(-28f, 28f)));
    }
}
