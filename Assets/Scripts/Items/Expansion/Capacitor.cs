[UnityEngine.Scripting.Preserve]
public sealed class Capacitor : AItem
{
    void Awake(){SetData("Capacitor","Ship equipment","Release fire to charge your next volley up to triple damage. Caffeine accelerates charging.",ItemType.passive);}
    public override void OnBegin(){}
    public override void OnPickup(){s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Capacitor);base.OnPickup();}
}
