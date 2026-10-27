using UnityEngine;
public sealed class ScatterCarrier : CombatRound
{
    int releases;
    // Plains carriers and the needles they release share one orange accent (Miguel, 2026-10-25).
    static readonly Color PlainsOrange = new Color(1, .58f, .08f);
    protected override void Initialize()
    {
        releases = 0;
        Orient();
        if (LevelRun.Current == LevelId.Fields)
            GetComponent<ProjectileSpriteTint>().SetAccent(PlainsOrange);
    }

    void Release(Vector3 at, Vector2 direction)
    {
        var needle = Launch<NeedleRound>(at, direction, 3, .11f);
        if (needle != null && LevelRun.Current == LevelId.Fields)
            needle.GetComponent<ProjectileSpriteTint>().SetAccent(PlainsOrange);
    }

    protected override void Tick(float dt)
    {
        base.Tick(dt);
        if (age > (releases + 1) * .78f)
        {
            releases++;
            var side = new Vector2(-heading.y, heading.x);
            float muzzle = transform.localScale.x * .435f;
            var center = transform.position - (Vector3)heading * transform.localScale.y * .022f;
            Release(center + (Vector3)side * muzzle, side);
            Release(center - (Vector3)side * muzzle, -side);
        }
    }
}
