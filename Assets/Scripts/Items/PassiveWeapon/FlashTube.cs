[UnityEngine.Scripting.Preserve]
public sealed class FlashTube : ASecondaryItem
{
    protected override SecondaryPattern Pattern => SecondaryPattern.Flash;
    void Awake() => SetData("Flash Tube", "Secondary fire", "Fires short laser pulses straight ahead.", ItemType.passiveWeapon);
}
