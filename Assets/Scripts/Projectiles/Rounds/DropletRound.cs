// A teardrop whose point trails behind it.
// Approved neutral pixels; the district hue and every motion program come from code.
public sealed class DropletRound : CombatRound
{
    protected override void Initialize()
    {
        transform.localScale *= 1.3f;
        Orient();
    }
}
