// A wrapped sweet tumbling end over end.
// Approved neutral pixels; the district hue and every motion program come from code.
public sealed class CandyRound : CombatRound
{
    protected override float Spin => 220;

    protected override void Initialize()
    {
        transform.localScale *= 1.55f;
        Orient();
    }
}
