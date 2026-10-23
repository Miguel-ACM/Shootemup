using UnityEngine;
using TMPro;
using System;

public sealed class ShipLoadout : MonoBehaviour
{
    Inputs input;
    public int SelectedWeapon {get;private set;}=-1;
    public int SelectedActive {get;private set;}=-1;
    public readonly AWeapon[] Weapons=new AWeapon[2];
    public readonly AActiveItem[] Actives=new AActiveItem[2];
    public int LastSelectedSlot {get;private set;}=-1;
    static readonly Type[] weaponTypes={typeof(NeedleCannon),typeof(BurstLauncher),typeof(BeamEmitter),typeof(DiscThrower),typeof(SwarmRack),typeof(ScatterCannon),typeof(FlameProjector),typeof(RailDriver)};
    void Awake()
    {
        input=new Inputs();
        input.PlayerControls.FirePrimary.started+=_=>SelectWeapon(0);
        input.PlayerControls.FireSecondary.started+=_=>SelectWeapon(1);
        input.PlayerControls.ActiveLeft.started+=_=>SelectActive(0);
        input.PlayerControls.ActiveRight.started+=_=>SelectActive(1);
        input.Enable();
    }
    void Start()
    {
        foreach(var w in GetComponentsInChildren<AWeapon>())if(w.Slot!=AWeapon.WeaponInput.both)Weapons[w.Slot==AWeapon.WeaponInput.primary ? 0 : 1]=w;
        gameObject.AddComponent<LoadoutHUD>();
    }
    public void ResetSelection(){SelectedWeapon=SelectedActive=LastSelectedSlot=-1;}
    void SelectWeapon(int slot){SelectedWeapon=slot;LastSelectedSlot=slot;}
    void SelectActive(int slot){SelectedActive=slot;LastSelectedSlot=slot+2;}
    public int SlotFor(AItem item)
    {
        if(item is AWeaponItem)return SelectedWeapon>=0 ? SelectedWeapon : Weapons[0]==null ? 0 : Weapons[1]==null ? 1 : -1;
        if(item is AActiveItem)return SelectedActive>=0 ? SelectedActive : Actives[0]==null ? 0 : Actives[1]==null ? 1 : -1;
        return 0;
    }
    public void EquipWeapon(string name,int slot)
    {
        slot=Mathf.Clamp(slot,0,1);
        AWeapon replacement=null;
        var prefab=Resources.Load<GameObject>("Weapons/"+name);
        if(prefab!=null)replacement=Instantiate(prefab,transform).GetComponent<AWeapon>();
        else
        {
            Type type=Array.Find(weaponTypes,t=>t.Name==name);
            if(type==null){Debug.LogError("Unknown weapon: "+name);return;}
            var go=new GameObject(name);go.transform.SetParent(transform,false);replacement=(AWeapon)go.AddComponent(type);
        }
        replacement.transform.localPosition=Vector3.zero;replacement.SetWeaponInput(slot==0 ? AWeapon.WeaponInput.primary : AWeapon.WeaponInput.secondary);
        var old=Weapons[slot];Weapons[slot]=replacement;
        if(old!=null){old.StopFiring();old.enabled=false;if(old.gameObject==gameObject)Destroy(old);else Destroy(old.gameObject);}

    }
    public void EquipActive(AActiveItem item,int slot)
    {
        slot=Mathf.Clamp(slot,0,1);var old=Actives[slot];Actives[slot]=item;
        var ship=GetComponent<Shipcontroller>();if(slot==0)ship.AddActiveLeft(item);else ship.AddActiveRight(item);
        if(old!=null && old!=item)Destroy(old.gameObject);
    }
    void OnDestroy(){input.Disable();input.Dispose();}
}
