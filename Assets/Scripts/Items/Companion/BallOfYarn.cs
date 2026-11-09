[UnityEngine.Scripting.Preserve]
public sealed class BallOfYarn : AItem
{
    void Awake()
    {
        SetData("Ball of Yarn", "Unravelling", "An orbiting ball whose thread catches bullets.", ItemType.partner);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        YarnBall.Attach(s);
        base.OnPickup();
    }
}
