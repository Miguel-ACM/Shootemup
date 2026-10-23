using UnityEngine;
public sealed class NeedleRound : CombatRound
{
    protected override void Initialize(){transform.localScale=Vector3.Scale(transform.localScale,new Vector3(.55f,1.9f,1));visual.color=new Color(1,.48f,.25f);Orient();}
}
