[UnityEngine.Scripting.Preserve]
public sealed class WindUpTeeth : ASecondaryItem
{
    protected override SecondaryPattern Pattern => SecondaryPattern.Teeth;
    void Awake() { SetData("Wind-up Teeth", "Secondary fire", "Alternating side bites. Focus brings them forward.", ItemType.passiveWeapon); }
}
