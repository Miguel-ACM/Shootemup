[UnityEngine.Scripting.Preserve]
public sealed class ReserveCell : AItem
{
    void Awake(){SetData("Reserve Cell","Ship equipment","Store up to 12 energy from excess pickups. The next successful active use draws on the reserve.",ItemType.passive);}
    public override void OnBegin(){}
    public override void OnPickup(){s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Reserve);base.OnPickup();}
}
