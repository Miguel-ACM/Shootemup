using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class CarouselItem : AWeaponItem
{
    void Awake()
    {
        weaponName = nameof(Carousel);
        SetData("Carousel", "Weapon", "Changes gun every 30 combat seconds. +15% damage.", ItemType.weapon);
    }
    public override void OnBegin() { }
}

public sealed class Carousel : AWeapon
{
    static readonly string[] forms = { nameof(ScatterCannon), nameof(RailDriver), nameof(BeamEmitter), nameof(SwarmRack) };
    AWeapon gun;
    Carousel source;
    int form;
    float remaining = 30;
    bool warned;
    public string Form => forms[form];
    public float SwapTurn => remaining < .8f ? Mathf.SmoothStep(0, 360, 1 - remaining / .8f) : 0;

    new void Start()
    {
        if (source != null) form = source.form;
        FitGun();
    }

    internal void MatchForm(Carousel original) => source = original;

    void Update()
    {
        if (!GameClock.ActivePlay) return;
        if (source != null)
        {
            if (form != source.form)
            {
                form = source.form;
                FitGun();
            }
            return;
        }
        remaining -= GameClock.PlayerDelta;
        if (!warned && remaining < .8f)
        {
            warned = true;
            ItemSound.Cue("itemFocus", volume: .13f, pitch: .85f);
        }
        if (remaining > 0) return;
        remaining += 30;
        warned = false;
        form = (form + 1) % forms.Length;
        FitGun();
        var input = GameInput.Snapshot;
        SyncHeldInput(input.Primary, input.Secondary);
        ItemSound.Cue("itemRelay", volume: .13f);
    }

    void FitGun()
    {
        if (gun != null)
        {
            gun.StopFiring();
            gun.enabled = false;
            Destroy(gun.gameObject);
        }
        gun = WeaponCatalog.Create(Form, transform);
        gun.SetWeaponInput(Slot);
        gun.EquipmentOwner = this;
        gun.IsCarbonCopy = IsCarbonCopy;
        gun.DamageMultiplier = 1.15f;
        gun.updateSpawnPoints();
    }

    public override void StopFiring()
    {
        base.StopFiring();
        if (gun != null) gun.StopFiring();
    }

    public override void SyncHeldInput(bool primary, bool secondary)
    {
        if (gun != null) gun.SyncHeldInput(primary, secondary);
    }
}
