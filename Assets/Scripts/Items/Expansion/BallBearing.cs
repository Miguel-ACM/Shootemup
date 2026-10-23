[UnityEngine.Scripting.Preserve]
public sealed class BallBearing : AItem
{
    void Awake(){SetData("Ball Bearing","Ship equipment","Periodically sprays three metal fragments from a direct hit. Fragments inherit guidance.",ItemType.passive);}
    public override void OnBegin(){}
    public override void OnPickup(){s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Bearing);base.OnPickup();}
}
