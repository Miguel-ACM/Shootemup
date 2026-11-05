[UnityEngine.Scripting.Preserve]
public sealed class WindUpTeeth : ASecondaryItem
{
    protected override SecondaryPattern Pattern => SecondaryPattern.Teeth;
    void Awake() { SetData("Wind-up Teeth", "Secondary fire", "Rapid five-shot bursts. Focus tightens the spread.", ItemType.passiveWeapon); }
}
