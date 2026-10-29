using System.Collections.Generic;
using UnityEngine;

// A charged impact leaves a bounded, stationary damage field, not another volley.
public sealed class ChargeBlast : MonoBehaviour
{
    const int Limit = 16;
    static readonly List<ChargeBlast> live = new List<ChargeBlast>();
    readonly List<AEnemy> targets = new List<AEnemy>();
    LineRenderer edge, core;
    float age, duration, radius, totalDamage, nextTick;

    public static void Spawn(Vector3 position, float damage, int level)
    {
        live.RemoveAll(blast => blast == null);
        ChargeBlast blast;
        if (live.Count >= Limit)
        {
            blast = live[0];
            live.RemoveAt(0);
        }
        else
        {
            blast = new GameObject("Lingering charge detonation").AddComponent<ChargeBlast>();
            blast.edge = CombatVisuals.Line(blast.transform, new Color(.25f, .7f, .8f), .055f);
            blast.core = CombatVisuals.Line(blast.transform, new Color(.4f, .8f, .8f), .09f);
            blast.core.positionCount = 49;
            blast.edge.sortingOrder = blast.core.sortingOrder = 13;
        }
        blast.transform.position = new Vector3(position.x, position.y, -1);
        blast.age = blast.nextTick = 0;
        blast.duration = level >= 3 ? 1.4f : .9f;
        blast.radius = (level >= 3 ? 1.35f : .85f) * Mathf.Sqrt(GameRules.playerShootSize);
        blast.totalDamage = damage;
        live.Add(blast);
        ItemProcVfx.Emit(ItemProcShape.Detonate, position, new Color(.3f, .75f, .8f), blast.radius);
        ItemSound.Cue("itemChargeBlast", "cannon", .14f, .8f, .16f);
    }

    void Update()
    {
        if (Menu.GameplayPaused || WorldTimeStop.Frozen || Time.deltaTime <= 0)
            return;
        if (GameRules.playerDisabled)
        {
            Destroy(gameObject);
            return;
        }
        age += Time.deltaTime;
        DrawField();
        // Fixed slices preserve total damage across frame rates, including the last slice.
        while (nextTick < duration && age >= nextTick)
        {
            float slice = Mathf.Min(.1f, duration - nextTick);
            float tickRadius = RadiusAt(nextTick);
            targets.Clear();
            targets.AddRange(AEnemy.Live);
            foreach (var enemy in targets)
            {
                if (enemy == null || !enemy.CanBeTargeted)
                    continue;
                var collider = enemy.GetComponent<Collider2D>();
                if (collider == null || !collider.enabled || (collider.ClosestPoint(transform.position) - (Vector2)transform.position).sqrMagnitude > tickRadius * tickRadius)
                    continue;
                float amount = totalDamage * slice / duration;
                float dealt = CombatUpgrades.Current != null ? CombatUpgrades.Current.DirectHit(enemy, amount, 1) : amount;
                enemy.HitAt(collider.ClosestPoint(transform.position), Vector2.up);
                enemy.damage(dealt);
                Score.IncreaseScore(Mathf.RoundToInt(dealt));
            }
            nextTick += slice;
        }
        if (age >= duration)
            Destroy(gameObject);
    }

    void DrawField()
    {
        float size = RadiusAt(age);
        float fade = Mathf.Clamp01((duration - age) / .2f);
        CombatVisuals.Ring(edge, transform.position, size);
        for (int i = 0; i < 49; i++)
        {
            float angle = i * Mathf.PI * 2 / 48;
            float ripple = .65f + .07f * Mathf.Cos(angle * 6 - age * 8) + .04f * Mathf.Sin(age * 25);
            core.SetPosition(i, transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * size * ripple);
        }
        edge.startColor = edge.endColor = new Color(.25f, .7f, .8f, .7f * fade);
        core.startColor = core.endColor = new Color(.4f, .8f, .8f, .3f * fade);
    }

    float RadiusAt(float t) => radius * Mathf.Lerp(.35f, 1, Mathf.Clamp01(t / .16f));
    void OnDestroy()
    {
        live.Remove(this);
    }
}
