using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class DeflectorPlate : AItem
{
    void Awake()
    {
        SetData("Deflector Plate", "Straight through", "Blocking companions redirect your shots straight ahead.", ItemType.passive);
    }

    public override void OnBegin() { }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.DeflectorPlate);
        base.OnPickup();
    }
}
