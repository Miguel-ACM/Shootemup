[UnityEngine.Scripting.Preserve]
public sealed class RibbonSpool : ASecondaryItem
{
    protected override SecondaryPattern Pattern => SecondaryPattern.Ribbon;
    void Awake() { SetData("Ribbon Spool", "Secondary fire", "A fast weaving stream. Focus pulls it straight.", ItemType.passiveWeapon); }
}
