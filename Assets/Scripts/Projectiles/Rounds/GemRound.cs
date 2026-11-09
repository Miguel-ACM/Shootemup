// A cut gem that stays upright so its facets keep their light.
// Approved neutral pixels; the district hue and every motion program come from code.
public sealed class GemRound : CombatRound
{
    protected override bool Upright => true;

    protected override void Initialize()
    {
        transform.localScale *= 1.15f;
        Orient();
    }
}
