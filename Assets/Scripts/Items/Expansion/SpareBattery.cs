[UnityEngine.Scripting.Preserve]
public sealed class SpareBattery : AItem
{
    void Awake(){SetData("Spare Battery","Ship equipment","Stores part of direct damage and releases a powerful seeking shot when full. Still works.",ItemType.passive);}
    public override void OnBegin(){}
    public override void OnPickup(){s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Battery);base.OnPickup();}
}
