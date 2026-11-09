[UnityEngine.Scripting.Preserve]
public sealed class PaperPlane : AItem
{
    void Awake()
    {
        SetData("Paper Plane", "Folded at the back of class", "Loops out through enemies and back.", ItemType.partner);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        PaperPlaneFlight.Attach(s);
        base.OnPickup();
    }
}
