using System.Collections.Generic;
using UnityEngine;

// Poison is slow and creeping: one heavy jade tick a second whose dose builds the longer
// the enemy stays poisoned, and a poisoned hull takes extra damage from everything,
// burn included. Burn is the fast, fierce counterpart (see BurningHull).
public sealed class PoisonedHull : MonoBehaviour
{
    const float TickRate = 1, Duration = 6, FirstDose = .5f, DoseGrowth = .15f, MaxDose = 1.8f;
    public const float WeaknessFactor = 1.15f;
    static readonly HashSet<AEnemy> weakened = new HashSet<AEnemy>();
    float expires, nextTick, potency;
    int doses;
    AEnemy enemy;
    HullAfflictionVfx effect;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetWeakened()
    {
        weakened.Clear();
    }

    /// <summary>The damage multiplier a poisoned enemy suffers from every source.</summary>
    public static float Weakness(AEnemy target) => target != null && weakened.Contains(target) ? WeaknessFactor : 1;

    public void Infect(float damage)
    {
        enemy = GetComponent<AEnemy>();
        potency = Mathf.Max(potency, Mathf.Clamp(damage * .35f, 1, 9));
        expires = Time.time + Duration;
        if (effect == null)
        {
            effect = HullAfflictionVfx.Acquire(enemy, true);
            weakened.Add(enemy);
            nextTick = Time.time + TickRate;
            ItemProcVfx.Emit(ItemProcShape.Toxic, transform.position, PlayerShotPalette.Poison, Mathf.Clamp(enemy.size * .6f, .2f, .6f));
            ItemSound.Cue("itemPoison", volume: .065f, cooldown: .3f);
        }
    }

    void Update()
    {
        if (Time.time >= expires)
        {
            Destroy(this);
            return;
        }
        effect?.Tick(1);
        if (enemy != null && enemy.CanBeTargeted && Time.deltaTime > 0 && Time.time >= nextTick)
        {
            nextTick = Time.time + TickRate;
            // Each dose is heavier than the last while the poison is kept topped up.
            doses++;
            float dose = potency * Mathf.Min(MaxDose, FirstDose + DoseGrowth * doses);
            CombatHit.Affliction(enemy, dose);
            effect?.Pulse(dose / (potency * MaxDose));
            ItemSound.Cue("itemPoison", volume: .03f, pitch: .8f + .05f * Mathf.Min(doses, 8), cooldown: .45f);
        }
    }

    void OnDestroy()
    {
        if (enemy != null)
            weakened.Remove(enemy);
        effect?.Dispose();
    }
}
