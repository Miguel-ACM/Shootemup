using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class Redline : OffensiveActiveItem
{
    void Awake()
    {
        SetData("Redline", "Past the stop", "Drive both guns faster for four seconds. 40 energy.", ItemType.active);
        cost = 40; cd = 9;
    }
    protected override OffensiveCast Launch() => HasWeapons && RedlineDrive.Current == null ? CreateCast<RedlineDrive>("Redline drive") : null;
}

[DefaultExecutionOrder(-70)]
public sealed class RedlineDrive : OffensiveCast
{
    public static RedlineDrive Current { get; private set; }
    ShipLoadout loadout;
    SpriteRenderer[] ports;
    float nextAccent;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() => Current = null;

    public static bool Drives(AWeapon gun)
    {
        var drive = Current;
        return drive != null && drive.enabled && drive.Owner != null && drive.Owner.health > 0 &&
            gun != null && (gun.IsCarbonCopy || (drive.loadout != null &&
            (drive.loadout.Weapons[0] == gun || drive.loadout.Weapons[1] == gun)));
    }

    protected override void Begin()
    {
        Current = this;
        loadout = Owner.GetComponent<ShipLoadout>();
        ports = new SpriteRenderer[2];
        for (int i = 0; i < 2; i++)
            ports[i] = ItemSpark.Sprite(transform, "Redline hot muzzle", "standard", new Color(.9f, .35f, .12f), .13f);
        ItemSound.Cue("overdrive", volume: .22f, pitch: 1.2f, cooldown: 0);
    }

    protected override void Step(float dt)
    {
        transform.position = Owner.transform.position;
        if (Age >= 4) { Finish(); return; }
        if (loadout != null)
            foreach (var weapon in loadout.Weapons) Drive(weapon);
        for (int i = 0; i < ports.Length; i++)
        {
            ports[i].transform.position = transform.position + new Vector3(i == 0 ? -.32f : .32f, .57f, -2);
            ports[i].color = Color.Lerp(new Color(.8f, .16f, .06f), new Color(.95f, .67f, .24f), Age / 4);
        }
        if (Age >= nextAccent)
        {
            nextAccent = Age + .22f;
            ItemSpark.Emit(transform.position + Vector3.up * .6f, new Color(.8f, .35f, .15f), 2, .08f, true);
        }
    }

    public static void Drive(AWeapon weapon)
    {
        if (weapon == null) return;
        // A charged gun releases at full charge, then starts the next shot on the next frame.
        bool held = weapon is not WindUpCannon cannon || cannon.Level < 3;
        weapon.SyncHeldInput(held, held);
    }

    void OnDestroy()
    {
        if (Current != this) return;
        Current = null;
        if (loadout != null)
            foreach (var weapon in loadout.Weapons)
                if (weapon != null)
                {
                    weapon.StopFiring();
                    weapon.SyncHeldInput(GameInput.Snapshot.Primary, GameInput.Snapshot.Secondary);
                }
        if (Owner != null && Owner.health > 0) ItemSound.Cue("powerdown", volume: .14f, cooldown: 0);
    }
}
