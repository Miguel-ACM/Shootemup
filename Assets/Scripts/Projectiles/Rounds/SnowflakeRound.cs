// A six-arm flake turning slowly as it drifts.
// Approved neutral pixels; the district hue and every motion program come from code.
public sealed class SnowflakeRound : CombatRound
{
    protected override float Spin => 70;

    protected override void Initialize()
    {
        transform.localScale *= 1.35f;
        Orient();
    }
}
