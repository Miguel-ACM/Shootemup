using UnityEngine;

// Tape Measure and Brass Knuckles scale a direct hit by how far the ship is from the part of
// the target that was struck: the tape rewards long shots, the knuckles point-blank ones.
// Strong hits mark themselves (a yellow tape tick, a brass knock) so the bonus can be read.
public static class ReachDamage
{
    public const float TapeBonus = .4f, KnuckleBonus = .5f;
    const float TapeNear = 3, TapeFar = 9, KnuckleClose = 1.4f, KnuckleFar = 3;
    static readonly Color Tape = new Color(1, .85f, .25f);
    static readonly Color Brass = new Color(.95f, .72f, .3f);

    public static float Scale(CombatUpgrades upgrades, AEnemy enemy, Vector3 ship)
    {
        int tape = upgrades.Count(UpgradeKind.TapeMeasure);
        int knuckles = upgrades.Count(UpgradeKind.BrassKnuckles);
        if (tape == 0 && knuckles == 0)
            return 1;
        enemy.ClosestDamagePoint(ship, out Vector2 struck);
        float distance = Vector2.Distance(ship, struck);
        float far = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(TapeNear, TapeFar, distance));
        float close = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(KnuckleClose, KnuckleFar, distance));
        float scale = 1 + tape * TapeBonus * far + knuckles * KnuckleBonus * close;
        if (tape > 0 && far > .6f && upgrades.Gate("tape", .3f))
        {
            RunInventory.Ping(UpgradeKind.TapeMeasure);
            ItemSpark.Emit(struck, Tape, 3, .07f);
        }
        if (knuckles > 0 && close > .7f && upgrades.Gate("knuckles", .22f))
        {
            RunInventory.Ping(UpgradeKind.BrassKnuckles);
            ItemProcVfx.Emit(ItemProcShape.Counter, struck, Brass, .3f);
            ItemSound.Cue("itemBearing", volume: .05f, pitch: .7f, cooldown: .22f);
        }
        return scale;
    }
}
