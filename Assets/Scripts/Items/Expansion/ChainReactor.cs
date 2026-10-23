[UnityEngine.Scripting.Preserve]
public sealed class ChainReactor : AItem
{
    void Awake(){SetData("Chain Reactor","Ship equipment","Burning enemies release a bounded fragment burst when destroyed. Combine with Lighter or the flamethrower.",ItemType.passive);}
    public override void OnBegin(){}
    public override void OnPickup(){s.GetComponent<CombatUpgrades>().Add(UpgradeKind.ChainReactor);base.OnPickup();}
}
