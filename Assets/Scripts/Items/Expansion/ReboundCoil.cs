[UnityEngine.Scripting.Preserve]
public sealed class ReboundCoil : AItem
{
    void Awake(){SetData("Rebound Coil","Ship equipment","Shots periodically redirect to a nearby second enemy. Fragments can rebound without splitting again.",ItemType.passive);}
    public override void OnBegin(){}
    public override void OnPickup(){s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Rebound);base.OnPickup();}
}
