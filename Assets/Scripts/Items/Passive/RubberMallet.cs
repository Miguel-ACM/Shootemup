using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class RubberMallet : AItem
{
    void Awake() => SetData("Rubber Mallet", "Give it a tap", "Shots knock small enemies back.", ItemType.passive);

    public override void OnBegin() { }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.RubberMallet);
        base.OnPickup();
    }

    public static void Nudge(AEnemy enemy, Vector2 incoming, int copies)
    {
        if (copies <= 0 || !enemy.IsSmallTarget || enemy is GroundVehicle || incoming.sqrMagnitude < .01f ||
            !enemy.TryGetComponent<AMover>(out var movement) || !movement.isActiveAndEnabled)
            return;
        movement.Kick(incoming.normalized * Mathf.Min(.65f, .42f + .12f * (copies - 1)));
        RunInventory.Ping(UpgradeKind.RubberMallet);
    }
}
