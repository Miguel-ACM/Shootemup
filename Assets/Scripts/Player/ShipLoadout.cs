using UnityEngine;
using TMPro;
using System;

public sealed class ShipLoadout : MonoBehaviour
{
    Inputs input;
    public AItem HoveredItem
    {
        get; private set;
    }

    int pickedFrame = -1;
    public bool ConsumedInputThisFrame => pickedFrame == Time.frameCount;
    public readonly AWeapon[] Weapons = new AWeapon[2];
    public readonly AActiveItem[] Actives = new AActiveItem[2];
    static readonly Type[] weaponTypes = { typeof(DepthCharge), typeof(Backdraft), typeof(WindUpCannon), typeof(NeedleCannon), typeof(BurstLauncher), typeof(BeamEmitter), typeof(DiscThrower), typeof(SwarmRack), typeof(ScatterCannon), typeof(FlameProjector), typeof(RailDriver) };
    void Awake()
    {
        input = new Inputs();
        input.PlayerControls.FirePrimary.started += _ => Choose(false, 0);
        input.PlayerControls.FireSecondary.started += _ => Choose(false, 1);
        input.PlayerControls.ActiveLeft.started += _ => Choose(true, 0);
        input.PlayerControls.ActiveRight.started += _ => Choose(true, 1);
        input.Enable();
    }

    void Start()
    {
        foreach (var w in GetComponentsInChildren<AWeapon>())
            if (w.Slot != AWeapon.WeaponInput.both)
                Weapons[w.Slot == AWeapon.WeaponInput.primary ? 0 : 1] = w;
        gameObject.AddComponent<LoadoutHUD>();
    }

    public void ResetSelection()
    {
        HoveredItem = null;
    }

    void Update()
    {
        RefreshHover();
    }

    void RefreshHover()
    {
        HoveredItem = null;
        var ship = GetComponent<Shipcontroller>();
        if (ship.health <= 0 || GameOverState.Active || Time.timeScale == 0)
            return;
        float closest = float.MaxValue;
        foreach (var item in AItem.Available)
        {
            if (item == null || !item.CanPick(ship) || SlotFor(item) >= 0)
                continue;
            float distance = Vector2.SqrMagnitude(item.transform.position - transform.position);
            if (distance < closest)
            {
                closest = distance;
                HoveredItem = item;
            }
        }
    }

    void Choose(bool active, int slot)
    {
        RefreshHover();
        if (HoveredItem == null || active != (HoveredItem is AActiveItem))
            return;
        var item = HoveredItem;
        pickedFrame = Time.frameCount;
        if (item.TryPick(slot))
            HoveredItem = null;
        else
            pickedFrame = -1;
    }

    public int SlotFor(AItem item)
    {
        if (item is AWeaponItem)
            return Weapons[0] == null ? 0 : Weapons[1] == null ? 1 : -1;
        if (item is AActiveItem)
            return Actives[0] == null ? 0 : Actives[1] == null ? 1 : -1;
        return 0;
    }

    public void EquipWeapon(string name, int slot)
    {
        slot = Mathf.Clamp(slot, 0, 1);
        AWeapon replacement = null;
        var prefab = Resources.Load<GameObject>("Weapons/" + name);
        if (prefab != null)
            replacement = Instantiate(prefab, transform).GetComponent<AWeapon>();
        else
        {
            Type type = Array.Find(weaponTypes, t => t.Name == name);
            if (type == null)
            {
                Debug.LogError("Unknown weapon: " + name);
                return;
            }
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            replacement = (AWeapon)go.AddComponent(type);
        }
        replacement.transform.localPosition = Vector3.zero;
        replacement.SetWeaponInput(slot == 0 ? AWeapon.WeaponInput.primary : AWeapon.WeaponInput.secondary);
        var old = Weapons[slot];
        Weapons[slot] = replacement;
        if (old != null)
        {
            old.StopFiring();
            old.enabled = false;
            if (old.gameObject == gameObject)
                Destroy(old);
            else
                Destroy(old.gameObject);
        }

    }

    public void EquipActive(AActiveItem item, int slot)
    {
        slot = Mathf.Clamp(slot, 0, 1);
        var old = Actives[slot];
        Actives[slot] = item;
        var ship = GetComponent<Shipcontroller>();
        if (slot == 0)
            ship.AddActiveLeft(item);
        else
            ship.AddActiveRight(item);
        if (old != null && old != item)
            Destroy(old.gameObject);
    }

    void OnDestroy()
    {
        input?.Disable();
        input?.Dispose();
    }
}


