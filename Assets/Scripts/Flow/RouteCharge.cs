using UnityEngine;

/// <summary>A continuous hold is required. The centre is neutral and switching sides starts a new hold.</summary>
public sealed class RouteCharge
{
    public const float HoldSeconds=2.6f;
    public const float NeutralWidth=.13f;
    public int Side { get; private set; }
    public float Progress { get; private set; }
    public bool Complete => Progress>=1;
    public void Step(float normalizedX,float deltaTime)
    {
        int next=normalizedX < -NeutralWidth ? -1 : normalizedX > NeutralWidth ? 1 : 0;
        StepSelection(next,deltaTime);
    }
    public void StepSelection(int next,float deltaTime)
    {
        if(next!=Side || next==0)Progress=0;
        Side=next;
        if(Side!=0)Progress=Mathf.Clamp01(Progress+Mathf.Max(0,deltaTime)/HoldSeconds);
    }
}
