using System.Collections.Generic;
using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class DemolitionPlunger : OffensiveActiveItem
{
    void Awake()
    {
        SetData("Demolition Plunger", "Everybody ready?", "Hits plant charges. Press again to detonate. 35 energy.", ItemType.active);
        cost = 35; cd = 7;
    }
    protected override OffensiveCast Launch() => CreateCast<DemolitionMarks>("Demolition charges");
    protected override bool Repress()
    {
        if (Cast is not DemolitionMarks marks || marks.Age < .15f) return false;
        marks.Detonate(); return true;
    }
}

public sealed class DemolitionMarks : OffensiveCast
{
    sealed class Mark
    {
        public AEnemy Enemy;
        public int Stacks;
        public LineRenderer Ring;
        public float Gate;
    }
    readonly List<Mark> marks = new List<Mark>(24);
    bool detonated;
    public int MarkCount => marks.Count;
    protected override void Begin()
    {
        CombatHit.PlayerHit += Record;
        ItemSound.Cue("capacitor", volume: .17f, pitch: .65f, cooldown: 0);
        ItemProcVfx.Emit(ItemProcShape.Lock, Owner.transform.position, new Color(.9f, .4f, .2f), .5f);
    }
    void Record(AEnemy enemy)
    {
        if (detonated || !GameClock.ActivePlay || enemy == null || !enemy.CanBeTargeted) return;
        Mark mark = marks.Find(entry => entry.Enemy == enemy);
        if (mark == null)
        {
            if (marks.Count >= 24) return;
            mark = new Mark { Enemy = enemy, Ring = CombatVisuals.Line(transform, new Color(.85f, .42f, .18f, .85f), .035f) };
            marks.Add(mark);
        }
        if (Age < mark.Gate) return;
        mark.Gate = Age + .08f;
        mark.Stacks = Mathf.Min(12, mark.Stacks + 1);
        ItemSound.Cue("itemFocus", volume: .06f, pitch: 1.25f, cooldown: .12f);
    }
    protected override void Step(float dt)
    {
        for (int i = marks.Count - 1; i >= 0; i--)
        {
            var mark = marks[i];
            if (mark.Enemy == null || !mark.Enemy.CanReceiveDamage || !mark.Enemy.CanBeTargeted)
            {
                Destroy(mark.Ring.gameObject); marks.RemoveAt(i); continue;
            }
            CombatVisuals.Ring(mark.Ring, mark.Enemy.transform.position, .25f + mark.Stacks * .018f);
        }
        if (Age >= 4) Detonate();
    }
    public void Detonate()
    {
        if (detonated) return;
        detonated = true;
        CombatHit.PlayerHit -= Record;
        // Snapshot positions before damage: multipart death may destroy neighbouring targets.
        foreach (var mark in marks)
        {
            var enemy = mark.Enemy;
            if (enemy == null || !enemy.CanReceiveDamage || !enemy.CanBeTargeted) continue;
            var p = enemy.transform.position;
            CombatHit.Apply(enemy, GameRules.playerShootDamage * (2 + mark.Stacks * 1.2f), p, Vector2.up, generation: 1);
            PSXFireballs.Shockwave(p, .65f + mark.Stacks * .035f, .3f, new Color(.9f, .48f, .23f), 2, .12f);
        }
        ItemSound.Cue("itemDepthBurst", volume: .26f, pitch: .65f, cooldown: 0);
        Finish();
    }
    void OnDestroy() => CombatHit.PlayerHit -= Record;
}
