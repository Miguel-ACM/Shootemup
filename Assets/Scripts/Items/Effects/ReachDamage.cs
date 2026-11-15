using UnityEngine;

// Tape Measure scales a direct hit by how far the ship is from the part of the target that
// was struck, rewarding long shots. Strong hits leave a yellow tape tick so the bonus can be read.
public static class ReachDamage
{
    public const float TapeBonus = .4f;
    const float TapeNear = 3, TapeFar = 9;
    static readonly Color Tape = new Color(1, .85f, .25f);

    public static float Scale(CombatUpgrades upgrades, AEnemy enemy, Vector3 ship)
    {
        int tape = upgrades.Count(UpgradeKind.TapeMeasure);
        if (tape == 0)
            return 1;
        enemy.ClosestDamagePoint(ship, out Vector2 struck);
        float distance = Vector2.Distance(ship, struck);
        float far = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(TapeNear, TapeFar, distance));
        float scale = 1 + tape * TapeBonus * far;
        if (tape > 0 && far > .6f && upgrades.Gate("tape", .3f))
        {
            RunInventory.Ping(UpgradeKind.TapeMeasure);
            ItemSpark.Emit(struck, Tape, 3, .07f);
        }
        return scale;
    }
}
