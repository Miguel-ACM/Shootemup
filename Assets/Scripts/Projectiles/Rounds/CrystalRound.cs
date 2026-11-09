// A faceted ice shard, pointing along its path.
// Approved neutral pixels; the district hue and every motion program come from code.
public sealed class CrystalRound : CombatRound
{
    protected override void Initialize()
    {
        transform.localScale *= 1.45f;
        Orient();
    }
}
