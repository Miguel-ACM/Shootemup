[UnityEngine.Scripting.Preserve]
public sealed class SplitterCore : AItem
{
    void Awake(){SetData("Splitter Core","Ship equipment","Periodically splits a direct hit into two weaker, non-splitting shots. Beam hits fork too.",ItemType.passive);}
    public override void OnBegin(){}
    public override void OnPickup(){s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Splitter);base.OnPickup();}
}
