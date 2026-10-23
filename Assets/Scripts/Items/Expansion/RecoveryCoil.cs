[UnityEngine.Scripting.Preserve]
public sealed class RecoveryCoil : AItem
{
    void Awake(){SetData("Recovery Coil","Ship equipment","Every eight blocked or erased rounds fire a seeking discharge. Includes a moon if you have none.",ItemType.passive);}
    public override void OnBegin(){}
    public override void OnPickup(){s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Recovery);base.OnPickup();}
}
