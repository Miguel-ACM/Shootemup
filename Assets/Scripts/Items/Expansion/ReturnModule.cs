[UnityEngine.Scripting.Preserve]
public sealed class ReturnModule : AItem
{
    void Awake(){SetData("Return Module","Ship equipment","Every fourth volley curves back for a second pass. Telekinesis helps the outward pass.",ItemType.passive);}
    public override void OnBegin(){}
    public override void OnPickup(){s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Return);base.OnPickup();}
}
