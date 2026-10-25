using UnityEngine;
public sealed class NeedleRound : CombatRound
{
    protected override void Initialize(){transform.localScale*=1.9f;Orient();}
}
