using UnityEngine;
using TMPro;
using System;

public sealed class ShipLoadout : MonoBehaviour
{
    public AItem HoveredItem
    {
        get; private set;
    }

    int pickedFrame = -1;
    public bool ConsumedInputThisFrame => pickedFrame == Time.frameCount;
    public readonly AWeapon[] Weapons = new AWeapon[2];
    public readonly AActiveItem[] Actives = new AActiveItem[2];
    void Awake()
    {
        _ = GameInput.Actions;
        GameInput.EquipmentSelection = Choose;
    }

    void Start()
    {
        foreach (var w in GetComponentsInChildren<AWeapon>())
            if (w.Slot != AWeapon.WeaponInput.both)
                Weapons[w.Slot == AWeapon.WeaponInput.primary ? 0 : 1] = w;
        ShipFrames.ApplyWeapons(this, ShipFrames.Current);
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

    bool Choose(bool active, int slot)
    {
        RefreshHover();
        if (!isActiveAndEnabled || HoveredItem == null || active != (HoveredItem is AActiveItem))
            return false;
        var item = HoveredItem;
        pickedFrame = Time.frameCount;
        if (item.TryPick(slot))
            HoveredItem = null;
        else
            pickedFrame = -1;
        return pickedFrame == Time.frameCount;
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
        var replacement = WeaponCatalog.Create(name, transform);
        if (replacement == null)
            return;
        replacement.transform.localPosition = Vector3.zero;
        replacement.SetWeaponInput(slot == 0 ? AWeapon.WeaponInput.primary : AWeapon.WeaponInput.secondary);
        RemoveWeapon(slot);
        Weapons[slot] = replacement;
    }

    /// <summary>The ship frame's own gun: the base cannon, another weapon by name, or none.</summary>
    public void SetPrimary(string name)
    {
        var current = Weapons[0];
        if (name == null)
        {
            RemoveWeapon(0);
            return;
        }
        if (current != null && current.GetType().Name == name)
            return;
        if (name != nameof(BaseWeapon))
        {
            EquipWeapon(name, 0);
            return;
        }
        RemoveWeapon(0);
        var gun = gameObject.AddComponent<BaseWeapon>();
        gun.SetWeaponInput(AWeapon.WeaponInput.primary);
        Weapons[0] = gun;
    }

    void RemoveWeapon(int slot)
    {
        var old = Weapons[slot];
        Weapons[slot] = null;
        if (old == null)
            return;
        old.StopFiring();
        old.enabled = false;
        if (old.gameObject == gameObject)
            Destroy(old);
        else
            Destroy(old.gameObject);
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
        if (GameInput.EquipmentSelection == Choose)
            GameInput.EquipmentSelection = null;
    }
}


