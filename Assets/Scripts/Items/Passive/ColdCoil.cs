using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class ColdCoil : AItem
{
    void Awake()
    {
        SetData("Cold Coil", "Cold current", "Lasers and electricity chill hulls and slow the rounds they fire.", ItemType.passive);
    }

    public override void OnBegin() { }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.ColdCoil);
        base.OnPickup();
    }
}
