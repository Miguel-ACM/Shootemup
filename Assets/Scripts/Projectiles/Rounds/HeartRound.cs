// A heart that always stays upright.
// Approved neutral pixels; the district hue and every motion program come from code.
public sealed class HeartRound : CombatRound
{
    protected override bool Upright => true;

    protected override void Initialize()
    {
        transform.localScale *= 1.2f;
        Orient();
    }
}
