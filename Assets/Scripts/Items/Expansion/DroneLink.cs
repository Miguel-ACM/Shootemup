[UnityEngine.Scripting.Preserve]
public sealed class DroneLink : AItem
{
    void Awake(){SetData("Drone Link","Ship equipment","Adds a support drone; all companions deal 20% more damage. Another Arm adds drone muzzles.",ItemType.passive);}
    public override void OnBegin(){}
    public override void OnPickup(){s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Drone);base.OnPickup();}
}
