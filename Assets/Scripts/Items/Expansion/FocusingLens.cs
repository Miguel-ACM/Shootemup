[UnityEngine.Scripting.Preserve]
public sealed class FocusingLens : AItem
{
    void Awake(){SetData("Focusing Lens","Ship equipment","Repeated direct hits on the same target increase damage up to 45%. Switching targets resets focus.",ItemType.passive);}
    public override void OnBegin(){}
    public override void OnPickup(){s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Focus);base.OnPickup();}
}
