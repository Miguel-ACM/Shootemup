[UnityEngine.Scripting.Preserve]
public sealed class PaperclipBox : ASecondaryItem
{
    protected override SecondaryPattern Pattern => SecondaryPattern.Paperclip;
    void Awake() { SetData("Paperclip Box", "Secondary fire", "Piercing side needles. Focus straightens their aim.", ItemType.passiveWeapon); }
}
