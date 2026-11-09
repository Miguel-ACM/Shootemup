// A five-point star that turns as it flies.
// Approved neutral pixels; the district hue and every motion program come from code.
public sealed class StarRound : CombatRound
{
    protected override float Spin => 150;

    protected override void Initialize()
    {
        transform.localScale *= 1.25f;
        Orient();
    }
}
