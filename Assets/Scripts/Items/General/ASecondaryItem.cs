public abstract class ASecondaryItem : AItem
{
    protected abstract SecondaryPattern Pattern { get; }
    public override void OnBegin() { }
    public override void OnPickup()
    {
        SecondaryShot.Attach(s, Pattern, GetType().Name);
        base.OnPickup();
    }
}
