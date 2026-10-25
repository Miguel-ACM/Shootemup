using UnityEngine;
public sealed class BurningHull : MonoBehaviour
{
    float expires, nextTick, power;
    AEnemy enemy;
    HullAfflictionVfx effect;
    bool trailEligible;
    float nextTrail;
    Vector3 lastTrail;
    public void Ignite(float amount, bool canLeaveTrail = true)
    {
        enemy = GetComponent<AEnemy>();
        trailEligible |= canLeaveTrail;
        power = Mathf.Min(12, power + amount * .35f);
        expires = Time.time + 2.5f;
        if (effect == null)
        {
            effect = new HullAfflictionVfx(enemy, false);
            lastTrail = transform.position;
        }
    }

    void Update()
    {
        if (Time.time > expires)
        {
            Destroy(this);
            return;
        }
        effect?.Tick();
        if (trailEligible && Time.deltaTime > 0 && !GameRules.playerDisabled && CombatUpgrades.Current != null && CombatUpgrades.Current.Count(UpgradeKind.HotSauce) > 0 && Time.time >= nextTrail && Vector2.Distance(lastTrail, transform.position) > .3f)
        {
            HotSauceTrail.Leave(enemy);
            nextTrail = Time.time + .18f;
            lastTrail = transform.position;
        }
        if (enemy != null && enemy.CanBeTargeted && Time.deltaTime > 0 && Time.time >= nextTick)
        {
            nextTick = Time.time + .4f;
            enemy.damage(power * .4f);
        }
    }

    void OnDestroy()
    {
        effect?.Dispose();
    }
}
