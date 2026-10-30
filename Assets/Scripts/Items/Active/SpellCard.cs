using UnityEngine;

// A nod to Touhou's bombs: a spell card spirals amulets out of the ship, dissolves every
// hostile round for its duration and turns each one into points. The ship is untouchable meanwhile.
[UnityEngine.Scripting.Preserve]
public sealed class SpellCard : AActiveItem
{
    void Awake()
    {
        SetData("Spell Card", "Declare it", "Clear the screen. Bullets become points.", ItemType.active);
        cost = 60;
        cd = 6;
    }

    public override float OnPress(float mana)
    {
        float valid = CheckValidPress(mana);
        if (valid < 0)
            return valid;
        if (s.GetComponent<SpellCardCast>() != null)
            return -2;
        s.gameObject.AddComponent<SpellCardCast>();
        lastCD = 0;
        return mana - cost;
    }
}

public sealed class SpellCardCast : MonoBehaviour
{
    public const float Duration = 1.6f;
    const int PointsPerRound = 10;
    const float ArmEvery = .045f;
    static readonly Color Vermilion = new Color(1f, .3f, .32f), Gold = new Color(1f, .85f, .4f);
    float elapsed, nextArm;
    int arm;
    readonly System.Collections.Generic.List<AEnemyBullet> cleared = new System.Collections.Generic.List<AEnemyBullet>();

    void Start()
    {
        var p = transform.position;
        ClearEffects.ScreenClear(Vermilion);
        PSXFireballs.Shockwave(p, 3.2f, .6f, Vermilion, 3, .3f);
        PSXFireballs.Shockwave(p, 1.6f, .45f, Gold, 2, .15f);
        PSXImpactEffects.ItemGet(p, Vermilion);
        ItemSound.Play("timeStop", .2f, 1.25f);
        SoundManager.PlaySound("blankbullet", .2f);
    }

    void Update()
    {
        if (GameRules.playerDisabled)
        {
            Destroy(this);
            return;
        }
        float dt = WorldTimeStop.PlayerDelta;
        elapsed += dt;
        Clear();
        // Two counter-rotating arms of amulets, Reimu-style, swirling out of the ship.
        while (nextArm <= elapsed && nextArm < Duration * .8f)
        {
            nextArm += ArmEvery;
            arm++;
            float turn = arm * 23;
            for (int side = 0; side < 4; side++)
            {
                Vector2 heading = CombatVisuals.Rotate(Vector2.up, side * 90 + (side % 2 == 0 ? turn : -turn));
                PlayerRound.Spawn(transform.position, heading, GameRules.playerShootDamage * 1.5f, 9, PlayerDelivery.Pulse, 1);
            }
        }
        if (elapsed >= Duration)
            Destroy(this);
    }

    void Clear()
    {
        cleared.Clear();
        foreach (var round in CombatRound.Live)
            if (BulletReflection.CanReflect(round))
                cleared.Add(round);
        foreach (var bullet in GameObject.FindGameObjectsWithTag("EnemyBullet"))
        {
            var round = bullet.GetComponent<AEnemyBullet>();
            if (round != null && !(round is CombatRound) && BulletReflection.CanReflect(round))
                cleared.Add(round);
        }
        foreach (var round in cleared)
        {
            round.Vanish();
            Score.IncreaseScore(PointsPerRound);
        }
    }
}
