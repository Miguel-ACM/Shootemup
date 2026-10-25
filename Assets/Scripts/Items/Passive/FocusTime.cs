[UnityEngine.Scripting.Preserve]
public sealed class FocusTime : AItem
{
    void Awake()
    {
        SetData("Focus time", "Quiet desk", "Move 40% slower while focusing.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        if (s.GetComponent<CombatUpgrades>().Count(UpgradeKind.FocusTime) == 0)
            s.slowMultiplier *= .6f;
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.FocusTime);
        base.OnPickup();
    }
}
