using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class HealthPacks : AActiveItem
{
    void Awake()
    {
        SetData("Health packs", "Field medicine", "Spend 30 energy to restore 20 health.", ItemType.active);
        cost = 30;
        cd = 3;
    }

    public override float OnPress(float mana)
    {
        float valid = CheckValidPress(mana);
        if (valid < 0)
            return valid;
        if (s.health <= 0 || s.health >= s.maxHealth)
            return -1;
        s.HealFlat(20);
        lastCD = 0;
        PSXFireballs.Shockwave(s.transform.position, .9f, .4f, new Color(.3f, .85f, .4f), 1, .06f);
        ItemSound.Play("heal", .2f);
        return mana - cost;
    }
}
