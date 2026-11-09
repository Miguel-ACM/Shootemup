// A crescent moon spinning as it travels.
// Approved neutral pixels; the district hue and every motion program come from code.
public sealed class CrescentRound : CombatRound
{
    protected override float Spin => 180;

    protected override void Initialize()
    {
        transform.localScale *= 1.25f;
        Orient();
    }
}
