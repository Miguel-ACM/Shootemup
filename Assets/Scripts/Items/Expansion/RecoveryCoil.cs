[UnityEngine.Scripting.Preserve]
public sealed class RecoveryCoil : AItem
{
    void Awake(){SetData("Recovery Coil","Ship equipment","Every 8 blocked bullets fire a homing shot.",ItemType.passive);}
    public override void OnBegin(){}
    public override void OnPickup(){s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Recovery);base.OnPickup();}
}
