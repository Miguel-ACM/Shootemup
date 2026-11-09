// A notched feather, rounded tip first.
// Approved neutral pixels; the district hue and every motion program come from code.
public sealed class FeatherRound : CombatRound
{
    protected override void Initialize()
    {
        transform.localScale *= 1.75f;
        Orient();
    }
}
