[UnityEngine.Scripting.Preserve]
public sealed class PocketMortar : ASecondaryItem
{
    protected override SecondaryPattern Pattern => SecondaryPattern.Mortar;
    void Awake() { SetData("Pocket Mortar", "Secondary fire", "Occasional shells burst into forward shrapnel.", ItemType.passiveWeapon); }
}
