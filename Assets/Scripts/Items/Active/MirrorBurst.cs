using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class MirrorBurst : AActiveItem
{
    void Awake()
    {
        SetData("Mirror Burst", "Active", "Turn 18 bullets into homing shots. 35 energy.", ItemType.active);
        cost = 35;
        cd = 3;
    }

    public override float OnPress(float mana)
    {
        float valid = CheckValidPress(mana);
        if (valid < 0)
            return valid;
        int converted = 0;
        foreach (var shot in FindObjectsByType<AEnemyBullet>())
        {
            if (converted >= 18)
                break;
            if (shot is PulseLaser || Vector2.Distance(shot.transform.position, s.transform.position) > 5)
                continue;
            PlayerRound.Spawn(shot.transform.position, Vector2.up, GameRules.playerShootDamage * 2, 9, PlayerDelivery.Missile, 1);
            shot.Vanish();
            CombatUpgrades.Current?.Block();
            converted++;
        }
        ClearEffects.RadiusClear(s.transform.position, 5, new Color(.3f, 1, .8f));
        lastCD = 0;
        return mana - cost;
    }
}
