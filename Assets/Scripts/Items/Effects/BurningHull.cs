using UnityEngine;

// Burn is fast and fierce: ten small ticks a second from stacking heat. Every hit feeds
// the heat; while the hits keep coming it holds, and once they stop it burns out within
// about a second. Poison is the slow, creeping counterpart (see PoisonedHull).
public sealed class BurningHull : MonoBehaviour
{
    const float TickRate = .1f, MaxHeat = 12, Hold = .5f, HalfLife = .45f;
    float heat, lastFed, nextTick;
    AEnemy enemy;
    HullAfflictionVfx effect;
    bool trailEligible;
    float nextTrail;
    Vector3 lastTrail;

    public float Heat => heat;

    public void Ignite(float amount, bool canLeaveTrail = true)
    {
        enemy = GetComponent<AEnemy>();
        trailEligible |= canLeaveTrail;
        heat = Mathf.Min(MaxHeat, heat + amount * .35f);
        lastFed = Time.time;
        if (effect == null)
        {
            effect = HullAfflictionVfx.Acquire(enemy, false);
            lastTrail = transform.position;
            nextTick = Time.time + TickRate;
            ItemProcVfx.Emit(ItemProcShape.Ignite, transform.position, PlayerShotPalette.Fire, Mathf.Clamp(enemy.size * .6f, .2f, .6f));
            ItemSound.Cue("itemBurn", volume: .06f, cooldown: .3f);
        }
    }

    void Update()
    {
        if (Time.deltaTime <= 0)
            return;
        if (Time.time - lastFed > Hold)
            heat *= Mathf.Pow(.5f, Time.deltaTime / HalfLife);
        if (heat < .05f)
        {
            Destroy(this);
            return;
        }
        effect?.Tick(heat / MaxHeat);
        if (trailEligible && !GameRules.playerDisabled && CombatUpgrades.Current != null && CombatUpgrades.Current.Count(UpgradeKind.HotSauce) > 0 && Time.time >= nextTrail && Vector2.Distance(lastTrail, transform.position) > .3f)
        {
            HotSauceTrail.Leave(enemy);
            nextTrail = Time.time + .18f;
            lastTrail = transform.position;
        }
        if (enemy != null && enemy.CanBeTargeted && Time.time >= nextTick)
        {
            nextTick = Time.time + TickRate;
            // The same damage per second as the heat value, delivered in quick quiet licks.
            CombatHit.Affliction(enemy, heat * TickRate * PoisonedHull.Weakness(enemy) * MatchStrike.BurnScale);
        }
    }

    void OnDestroy()
    {
        effect?.Dispose();
    }
}
