using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class DeadLetterItem : AWeaponItem
{
    void Awake()
    {
        weaponName = nameof(DeadLetter);
        SetData("Dead Letter", "Weapon", "Hold to leave six shots in place. Release to launch them upward.", ItemType.weapon);
    }
    public override void OnBegin() { }
}

public sealed class DeadLetter : AWeaponAutomatic
{
    readonly SpriteRenderer[] marks = new SpriteRenderer[6];
    int queued;
    // Six letters whatever the shot count: it posts them faster with fire rate.
    protected override WeaponAffinity Affinity => new WeaponAffinity(1.25f, 1.1f, .6f);
    void Awake() => shootSpeedModifier = .54f;

    public override void Shoot()
    {
        if (queued == marks.Length || !KeepFiring()) return;
        if (marks[queued] == null)
            marks[queued] = ItemSpark.Sprite(transform, "Held Dead Letter round", "standard", new Color(.66f, .84f, 1), .2f);
        var mark = marks[queued++];
        mark.transform.SetParent(null, true);
        mark.transform.position = transform.position + new Vector3(0, .55f, -1);
        mark.enabled = true;
        ItemSound.Cue("itemFocus", volume: .06f, pitch: .9f + queued * .08f);
    }

    protected override void WeaponReleased(bool primary)
    {
        bool released = EffectiveInput == WeaponInput.both ? isStopingPrimary && isStopingSecondary :
            primary == (EffectiveInput == WeaponInput.primary);
        if (!released || queued == 0) return;
        if (GameClock.ActivePlay)
        {
            float charge = CombatUpgrades.Current?.Volley() ?? 1;
            float damage = GameRules.playerShootDamage * 4.5f * charge * Affinity.DamageFactor(0);
            for (int i = 0; i < queued; i++)
                MagnifyingLens.Enlarge(ChargedShotCue.Apply(PlayerRound.WeaponShot(marks[i].transform.position, Vector2.up,
                    damage, GameRules.playerShootTravelSpeed, PlayerDelivery.Needle), charge));
            SoundManager.PlaySound("shotNeedle", .1f);
            GetComponentInParent<PSXShipEffects>()?.Shot();
        }
        Clear();
    }

    public override void StopFiring()
    {
        base.StopFiring();
        Clear();
    }

    void Clear()
    {
        for (int i = 0; i < queued; i++) marks[i].enabled = false;
        queued = 0;
    }

    void OnDisable()
    {
        foreach (var mark in marks)
            if (mark != null) Destroy(mark.gameObject);
        queued = 0;
    }
}
