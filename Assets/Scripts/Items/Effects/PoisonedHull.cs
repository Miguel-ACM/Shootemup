using UnityEngine;
public sealed class PoisonedHull : MonoBehaviour
{
    float expires, nextTick, power;
    AEnemy enemy;
    HullAfflictionVfx effect;
    public void Infect(float damage)
    {
        enemy = GetComponent<AEnemy>();
        power = Mathf.Max(power, Mathf.Clamp(damage * .35f, 1, 9));
        expires = Time.time + 5;
        if (effect == null)
        {
            effect = HullAfflictionVfx.Acquire(enemy, true);
            ItemProcVfx.Emit(ItemProcShape.Toxic, transform.position, PlayerShotPalette.Poison, Mathf.Clamp(enemy.size * .6f, .2f, .6f));
            ItemSound.Cue("itemPoison", volume: .065f, cooldown: .3f);
        }
        if (nextTick == 0)
            nextTick = Time.time + .5f;
    }

    void Update()
    {
        if (Time.time >= expires)
        {
            Destroy(this);
            return;
        }
        effect?.Tick();
        if (enemy != null && enemy.CanBeTargeted && Time.deltaTime > 0 && Time.time >= nextTick)
        {
            nextTick = Time.time + .5f;
            enemy.damage(power * .5f);
        }
    }

    void OnDestroy()
    {
        effect?.Dispose();
    }
}
