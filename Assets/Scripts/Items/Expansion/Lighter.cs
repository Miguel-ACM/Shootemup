[UnityEngine.Scripting.Preserve]
public sealed class Lighter : AItem
{
    void Awake(){SetData("Lighter","Ship equipment","Repeated hits build a short stacking burn. Watch the enemy engines spark.",ItemType.passive);}
    public override void OnBegin(){}
    public override void OnPickup(){s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Burn);base.OnPickup();}
}
