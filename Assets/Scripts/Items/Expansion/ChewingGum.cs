using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class ChewingGum : AItem
{
    void Awake(){SetData("Chewing gum","Stick around","Shots stick, swell, then pop.",ItemType.passive);}
    public override void OnBegin(){}
    public override void OnPickup(){s.GetComponent<CombatUpgrades>().Add(UpgradeKind.ChewingGum);base.OnPickup();}
}
