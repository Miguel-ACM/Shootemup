[UnityEngine.Scripting.Preserve]
public sealed class SalvageConverter : AItem
{
    void Awake(){SetData("Salvage Converter","Ship equipment","Every five collected coins fire a seeking energy shot. Magnet helps feed it.",ItemType.passive);}
    public override void OnBegin(){}
    public override void OnPickup(){s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Salvage);base.OnPickup();}
}
