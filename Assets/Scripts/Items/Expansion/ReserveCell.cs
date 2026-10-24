[UnityEngine.Scripting.Preserve]
public sealed class ReserveCell : AItem
{
    void Awake(){SetData("Reserve Cell","Ship equipment","Store 12 spare energy for actives.",ItemType.passive);}
    public override void OnBegin(){}
    public override void OnPickup(){s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Reserve);base.OnPickup();}
}
