using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class CarbonPaper : OffensiveActiveItem
{
    void Awake()
    {
        SetData("Carbon Paper", "In triplicate", "Two copies fire at 25% damage for five seconds. Copies trailing drones when unarmed. 50 energy.", ItemType.active);
        cost = 50; cd = 11;
    }
    protected override OffensiveCast Launch() => CarbonCopies.Current == null && (HasWeapons || s.GetComponentInChildren<TrailDrone>() != null)
        ? CreateCast<CarbonCopies>("Carbon copies") : null;
}

[DefaultExecutionOrder(-60)]
public sealed class CarbonCopies : OffensiveCast
{
    public static CarbonCopies Current { get; private set; }
    internal const float CopyPower = .25f;
    static Material projection;
    readonly Transform[] copies = new Transform[2];
    readonly GameObject[] droneWings = new GameObject[2];
    readonly AWeapon[,] guns = new AWeapon[2, 2];
    readonly AWeapon[] originals = new AWeapon[2];
    readonly System.Collections.Generic.List<Renderer> renderers = new System.Collections.Generic.List<Renderer>();
    MaterialPropertyBlock appearance;
    ShipLoadout loadout;
    int builtFrame;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { Current = null; }

    protected override void Begin()
    {
        Current = this;
        appearance = new MaterialPropertyBlock();
        loadout = Owner.GetComponent<ShipLoadout>();
        if (projection == null) projection = new Material(Resources.Load<Shader>("ShipDecoy")) { name = "Carbon paper projections" };
        for (int side = 0; side < 2; side++)
        {
            copies[side] = new GameObject("Carbon " + (side == 0 ? "port" : "starboard")).transform;
            copies[side].SetParent(transform, false);
            var display = ShipFrames.BuildDisplay(Owner, ShipFrames.Current);
            if (display == null) continue;
            display.transform.SetParent(copies[side], false);
            foreach (var renderer in display.GetComponentsInChildren<MeshRenderer>())
            {
                var materials = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < materials.Length; i++) materials[i] = projection;
                renderer.sharedMaterials = materials;
                renderer.sortingLayerName = "Player";
                renderer.sortingOrder = 0;
                renderers.Add(renderer);
            }
        }
        SyncWeapons(true);
        ItemSound.Cue("decoy", volume: .2f, pitch: 1.3f, cooldown: 0);
    }

    void SyncWeapons(bool initial = false)
    {
        if (loadout == null) return;
        bool changed = initial;
        for (int slot = 0; slot < 2; slot++)
        {
            var source = loadout.Weapons[slot];
            if (ReferenceEquals(source, originals[slot])) continue;
            changed = true;
            originals[slot] = source;
            for (int side = 0; side < 2; side++)
            {
                if (guns[side, slot] != null)
                {
                    guns[side, slot].StopFiring();
                    guns[side, slot].enabled = false;
                    Destroy(guns[side, slot].gameObject);
                }
                guns[side, slot] = null;
                if (source == null) continue;
                AWeapon copy;
                if (source is BaseWeapon)
                {
                    var root = new GameObject("Carbon base cannon");
                    root.transform.SetParent(copies[side], false);
                    copy = root.AddComponent<BaseWeapon>();
                }
                else copy = WeaponCatalog.Create(source is Electricity ? "ElectricityWeapon" : source.GetType().Name, copies[side]);
                if (copy == null) continue;
                copy.IsCarbonCopy = true;
                if (copy is Carousel carousel && source is Carousel original) carousel.MatchForm(original);
                copy.SetWeaponInput(slot == 0 ? AWeapon.WeaponInput.primary : AWeapon.WeaponInput.secondary);
                copy.updateSpawnPoints();
                guns[side, slot] = copy;
                builtFrame = Time.frameCount;
            }
        }
        if (!changed) return;
        var drones = originals[0] == null && originals[1] == null ? Owner.GetComponentsInChildren<TrailDrone>() : null;
        for (int side = 0; side < copies.Length; side++)
        {
            if (droneWings[side] != null)
            {
                droneWings[side].SetActive(false);
                Destroy(droneWings[side]);
                droneWings[side] = null;
            }
            if (drones == null || drones.Length == 0) continue;
            var wing = new GameObject("Carbon drone wing");
            wing.transform.SetParent(copies[side], false);
            droneWings[side] = wing;
            foreach (var drone in drones)
            {
                var copy = TrailDrone.Attach(Owner, drone.Laser, wing.transform, CopyPower);
                var hull = copy.GetComponent<MeshRenderer>();
                hull.sharedMaterial = projection;
                renderers.Add(hull);
            }
        }
    }

    protected override void Step(float dt)
    {
        if (Age >= 5) { Finish(); return; }
        transform.position = Owner.transform.position;
        SyncWeapons();
        for (int side = 0; side < 2; side++)
        {
            var position = Owner.transform.position + new Vector3(side == 0 ? -1.15f : 1.15f, -.12f, 0);
            position.x = Mathf.Clamp(position.x, Camera.LeftLimit + .35f, Camera.RightLimit - .35f);
            copies[side].position = position;
            if (Time.frameCount <= builtFrame) continue; // Let weapon Start install its normal controls first.
            for (int slot = 0; slot < 2; slot++)
            {
                var gun = guns[side, slot];
                if (gun == null) continue;
                if (RedlineDrive.Drives(gun)) RedlineDrive.Drive(gun);
                else gun.SyncHeldInput(GameInput.Snapshot.Primary, GameInput.Snapshot.Secondary);
            }
        }
        appearance.SetFloat("_Fade", Mathf.Min(1, Age / .2f, (5 - Age) / .4f) * .65f);
        appearance.SetFloat("_Phase", Age);
        foreach (var renderer in renderers) if (renderer != null) renderer.SetPropertyBlock(appearance);
    }

    void OnDestroy()
    {
        foreach (var gun in guns) if (gun != null) gun.StopFiring();
        if (Current == this) Current = null;
    }
}
