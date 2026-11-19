using UnityEngine;

public sealed class PoisonedHull : MonoBehaviour
{
    const float TickRate = .75f, Duration = 2.65f;
    float expires, nextTick, potency;
    int doses;
    AEnemy enemy;
    HullAfflictionVfx effect;

    public void Infect(float damage)
    {
        enemy = GetComponent<AEnemy>();
        if (Time.time >= expires)
            ClearDose();
        enabled = true;
        potency = Mathf.Max(potency, Mathf.Clamp(damage * .35f, 1, 9));
        expires = Time.time + Duration;
        if (effect == null)
        {
            effect = HullAfflictionVfx.Acquire(enemy, true);
            nextTick = Time.time + TickRate;
            ItemProcVfx.Emit(ItemProcShape.Toxic, transform.position, PlayerShotPalette.Poison, Mathf.Clamp(enemy.size * .6f, .2f, .6f));
            ItemSound.Cue("itemPoison", volume: .065f, cooldown: .3f);
        }
    }

    void Update()
    {
        if (Time.time >= expires)
        {
            ClearDose();
            enabled = false;
            return;
        }
        effect?.Tick(1);
        if (enemy != null && enemy.CanBeTargeted && Time.deltaTime > 0 && Time.time >= nextTick)
        {
            nextTick = Time.time + TickRate;
            // Each dose is heavier than the last while the poison is kept topped up.
            float dose = potency * (.24f + .055f * doses++);
            CombatHit.Affliction(enemy, dose, DamageKind.Poison);
            effect?.Pulse(Mathf.Clamp01(dose / (potency * 2)));
            ItemSound.Cue("itemPoison", volume: .03f, pitch: .8f + .05f * Mathf.Min(doses, 8), cooldown: .45f);
        }
    }

    void ClearDose()
    {
        doses = 0;
        potency = 0;
        effect?.Dispose();
        effect = null;
    }

    void OnDestroy() => ClearDose();
}
