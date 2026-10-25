[UnityEngine.Scripting.Preserve]
public sealed class GasolineLeak : AItem
{
    void Awake(){SetData("Gasoline leak","Handle with care","Red bullets set enemies alight.",ItemType.passive);}
    public override void OnBegin(){}
    public override void OnPickup(){s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Gasoline);base.OnPickup();}
}
