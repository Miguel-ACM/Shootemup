[UnityEngine.Scripting.Preserve]
public sealed class Lighter : AItem
{
    void Awake(){SetData("Lighter","Ship equipment","Hits apply stacking burn.",ItemType.passive);}
    public override void OnBegin(){}
    public override void OnPickup(){s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Burn);base.OnPickup();}
}
