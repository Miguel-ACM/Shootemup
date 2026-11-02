[UnityEngine.Scripting.Preserve]
public sealed class SewingKit : ASecondaryItem
{
    protected override SecondaryPattern Pattern => SecondaryPattern.Sewing;
    void Awake() { SetData("Sewing Kit", "Secondary fire", "Rapid weak needles. Focus joins the seam.", ItemType.passiveWeapon); }
}
