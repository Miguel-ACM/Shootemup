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
    TextMeshPro label;
    float showUntil;
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
        var go=new GameObject("Loadout selection over ship");go.transform.SetParent(transform,false);go.transform.localPosition=new Vector3(0,1.5f,-4);
        label=go.AddComponent<TextMeshPro>();label.fontSize=2.3f;label.alignment=TextAlignmentOptions.Center;label.rectTransform.sizeDelta=new Vector2(6,2);
        label.GetComponent<MeshRenderer>().sortingLayerName="Above UI";label.color=new Color(.7f,1,.9f);
    }
    public void ResetSelection(){SelectedWeapon=SelectedActive=-1;showUntil=Time.time+3;}
    void SelectWeapon(int slot){SelectedWeapon=slot;showUntil=Time.time+2;}
    void SelectActive(int slot){SelectedActive=slot;showUntil=Time.time+2;}
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
        showUntil=Time.time+3;
    }
    public void EquipActive(AActiveItem item,int slot)
    {
        slot=Mathf.Clamp(slot,0,1);var old=Actives[slot];Actives[slot]=item;
        var ship=GetComponent<Shipcontroller>();if(slot==0)ship.AddActiveLeft(item);else ship.AddActiveRight(item);
        if(old!=null && old!=item)Destroy(old.gameObject);showUntil=Time.time+3;
    }
    void Update()
    {
        if(label==null)return;
        label.enabled=CombatDecision.Active || Time.time<showUntil;
        string W(int i)=>Weapons[i]!=null ? Weapons[i].GetType().Name : "empty";
        string A(int i)=>Actives[i]!=null ? Actives[i].GetData()[0] : "empty";
        string Mark(bool selected)=>selected ? "<color=#FFE078>" : "<color=#B6DFDA>";
        label.text=Mark(SelectedWeapon==0)+"Z/RB "+W(0)+"</color>   "+Mark(SelectedWeapon==1)+"X/LB "+W(1)+"</color>\n"+
            Mark(SelectedActive==0)+"A/LT "+A(0)+"</color>   "+Mark(SelectedActive==1)+"S/RT "+A(1)+"</color>";
        if(CombatDecision.Active)label.text+="\nPress a slot before collecting to replace it";
    }
    void OnDestroy(){input.Disable();input.Dispose();}
}
