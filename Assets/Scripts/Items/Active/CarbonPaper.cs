using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class CarbonPaper : OffensiveActiveItem
{
    void Awake()
    {
        SetData("Carbon Paper", "In triplicate", "Two copies fire your weapons for five seconds. 50 energy.", ItemType.active);
        cost = 50; cd = 11;
    }
    protected override OffensiveCast Launch() => HasWeapons && CarbonCopies.Current == null ? CreateCast<CarbonCopies>("Carbon copies") : null;
}

[DefaultExecutionOrder(-60)]
public sealed class CarbonCopies : OffensiveCast
{
    public static CarbonCopies Current { get; private set; }
    static Material projection;
    readonly Transform[] copies = new Transform[2];
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
        SyncWeapons();
        ItemSound.Cue("decoy", volume: .2f, pitch: 1.3f, cooldown: 0);
    }

    void SyncWeapons()
    {
        if (loadout == null) return;
        for (int slot = 0; slot < 2; slot++)
        {
            var source = loadout.Weapons[slot];
            if (ReferenceEquals(source, originals[slot])) continue;
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
                else copy = WeaponCatalog.Create(source.GetType().Name, copies[side]);
                if (copy == null) continue;
                copy.IsCarbonCopy = true;
                copy.SetWeaponInput(slot == 0 ? AWeapon.WeaponInput.primary : AWeapon.WeaponInput.secondary);
                copy.updateSpawnPoints();
                guns[side, slot] = copy;
                builtFrame = Time.frameCount;
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
