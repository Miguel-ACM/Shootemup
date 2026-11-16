[UnityEngine.Scripting.Preserve]
public sealed class SideVents : ASecondaryItem
{
    protected override SecondaryPattern Pattern => SecondaryPattern.Vents;
    void Awake() => SetData("Side Vents", "Secondary fire", "Fires straight out to both sides.", ItemType.passiveWeapon);
}
