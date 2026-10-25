using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class WindUpCannon : AWeapon
{
    public float Charge
    {
        get; private set;
    }

    public int Level => Charge >= 1.2f ? 3 : Charge >= .45f ? 2 : 1;
    public bool Charging => isShootingPrimary || isShootingSecondary;
    float cooldown;
    int previousLevel;
    SpriteRenderer core;
    AudioSource winding;
    void Awake()
    {
        weaponType = WeaponType.charge;
    }

    public override void WeaponPressed(bool primary)
    {
        if (cooldown > 0 || GameRules.playerDisabled)
            return;
        bool wasCharging = Charging;
        base.WeaponPressed(primary);
        if (wasCharging || !Charging)
            return;
        Charge = 0;
        previousLevel = 1;
        if (core == null)
            core = ItemSpark.Sprite(transform, "Winding cannon muzzle", "standard", new Color(.45f, .85f, .8f), .14f);
        core.transform.position = transform.position + new Vector3(0, .5f, -2);
        core.enabled = true;
        if (winding == null)
        {
            winding = gameObject.AddComponent<AudioSource>();
            winding.playOnAwake = false;
            winding.spatialBlend = 0;
            winding.volume = .18f;
            var manager = FindAnyObjectByType<SoundManager>();
            if (manager != null)
                winding.outputAudioMixerGroup = manager.GetComponent<AudioSource>().outputAudioMixerGroup;
            winding.clip = ItemSound.Clip("windup");
        }
        winding.pitch = .8f;
        winding.Play();
    }

    void Update()
    {
        if (GameRules.playerDisabled)
        {
            StopFiring();
            return;
        }
        if (Menu.GameplayPaused)
        {
            if (winding != null)
                winding.Pause();
            return;
        }
        float dt = WorldTimeStop.PlayerDelta;
        if (dt <= 0)
            return;
        cooldown = Mathf.Max(0, cooldown - dt);
        if (!Charging)
            return;
        bool held = EffectiveInput == WeaponInput.primary ? !isStopingPrimary : EffectiveInput == WeaponInput.secondary ? !isStopingSecondary : !isStopingPrimary || !isStopingSecondary;
        if (!held)
        {
            Release();
            return;
        }
        Charge = Mathf.Min(1.2f, Charge + dt * MagnifyingLens.Rate * GameRules.playerShootSpeed / 10f);
        winding.UnPause();
        winding.pitch = .8f + Charge * .4f;
        core.transform.position = transform.position + new Vector3(0, .5f, -2);
        float worldSize = .1f + .16f * Charge / 1.2f;
        core.transform.localScale = new Vector3(worldSize / transform.lossyScale.x, worldSize / transform.lossyScale.y, 1);
        core.color = new Color(1, 1, 1, .65f + .15f * Mathf.Sin(Charge * 36));
        if (Level != previousLevel)
        {
            previousLevel = Level;
            ItemSound.Play("ready", .15f, Level == 3 ? 1.25f : 1);
            ItemSpark.Emit(core.transform.position, new Color(.5f, .9f, .85f), Level + 1, .1f, true);
            if (Level == 3)
                winding.Stop();
        }
    }

    void Release()
    {
        int level = Level;
        float power = level == 3 ? 16 : level == 2 ? 7 : 2;
        float capacitor = CombatUpgrades.Current != null ? CombatUpgrades.Current.Volley() : 1;
        foreach (var muzzle in spawnPoints)
        {
            var shot = PlayerRound.Spawn(transform.position + muzzle, Vector2.up, GameRules.playerShootDamage * power * capacitor, GameRules.playerShootTravelSpeed * 1.7f, PlayerDelivery.Needle);
            if (shot == null)
                continue;
            shot.pierces = level == 3 ? 5 : level == 2 ? 2 : 0;
            shot.GetComponent<SpriteRenderer>().sprite = ProjectileSprites.Load("needle");
            shot.transform.localScale *= level == 3 ? 1.5f : level == 2 ? 1.2f : .85f;
            MagnifyingLens.Enlarge(shot);
            ChargedShotCue.Apply(shot, capacitor);
        }
        GetComponentInParent<PSXShipEffects>()?.Shot();
        ItemSound.Play("cannon", level == 3 ? .28f : .17f, 1.25f - level * .12f);
        ItemSpark.Emit(transform.position + Vector3.up * .9f, new Color(.45f, .85f, .8f), level + 2, .13f, true);
        StopFiring();
        cooldown = .16f;
    }

    public override void StopFiring()
    {
        base.StopFiring();
        Charge = 0;
        if (core != null)
            core.enabled = false;
        if (winding != null)
            winding.Stop();
    }

    void OnDisable()
    {
        StopFiring();
    }
}
