using UnityEngine;

public sealed class ChilledHull : MonoBehaviour
{
    AEnemy enemy;
    HullAfflictionVfx finish;
    float coilUntil, ventUntil;
    public float Movement => enemy is DistrictBoss ? Mathf.Max(.9f, Factor) : Factor;
    public float Projectiles => enemy is DistrictBoss ? Mathf.Max(.82f, Factor) : Factor;
    float Factor => ventUntil > Time.time ? .55f : coilUntil > Time.time ? .72f : 1;

    public static void Apply(AEnemy target, float seconds, bool vent = false)
    {
        if (target is SandWormSection section) target = section.owner;
        if (target == null || !target.CanReceiveDamage) return;
        var chill = target.Chill ?? target.gameObject.AddComponent<ChilledHull>();
        target.Chill = chill;
        chill.enemy = target;
        if (vent)
            chill.ventUntil = Time.time + seconds;
        else
        {
            if (chill.coilUntil <= Time.time)
                RunInventory.Ping(UpgradeKind.ColdCoil);
            chill.coilUntil = Time.time + seconds;
        }
        chill.finish ??= HullAfflictionVfx.Acquire(target, HullAfflictionVfx.Frost);
    }

    void Update()
    {
        if (enemy == null || !enemy.CanReceiveDamage || Factor >= 1)
        {
            // Clear synchronously so a fresh hit late this frame creates its own status.
            enabled = false;
            Destroy(this);
        }
    }

    void OnDisable()
    {
        if (enemy != null && enemy.Chill == this)
            enemy.Chill = null;
        finish?.Dispose();
        finish = null;
    }
}
