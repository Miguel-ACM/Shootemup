// A tongue of fire whose flicks trail behind it.
// Approved neutral pixels; the district hue and every motion program come from code.
public sealed class FlameRound : CombatRound
{
    protected override void Initialize()
    {
        transform.localScale *= 1.35f;
        Orient();
    }
}
