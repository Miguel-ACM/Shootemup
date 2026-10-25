[UnityEngine.Scripting.Preserve]
public sealed class Icicle : AItem
{
    void Awake(){SetData("Icicle","Cold snap","Three hits build ice. The next hit shatters it into fragments.",ItemType.passive);}
    public override void OnBegin(){}
    public override void OnPickup(){s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Icicle);base.OnPickup();}
}
