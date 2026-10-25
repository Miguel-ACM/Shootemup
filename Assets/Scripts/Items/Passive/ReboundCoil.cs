[UnityEngine.Scripting.Preserve]
public sealed class ReboundCoil : AItem
{
    void Awake(){SetData("Rebound Coil","Ship equipment","Shots sometimes bounce to another enemy.",ItemType.passive);}
    public override void OnBegin(){}
    public override void OnPickup(){s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Rebound);base.OnPickup();}
}
