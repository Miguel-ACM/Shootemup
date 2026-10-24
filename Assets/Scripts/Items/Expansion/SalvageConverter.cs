[UnityEngine.Scripting.Preserve]
public sealed class SalvageConverter : AItem
{
    void Awake(){SetData("Salvage Converter","Ship equipment","Every 5 coins fire a homing shot.",ItemType.passive);}
    public override void OnBegin(){}
    public override void OnPickup(){s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Salvage);base.OnPickup();}
}
