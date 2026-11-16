[UnityEngine.Scripting.Preserve]
public sealed class Spitball : ASecondaryItem
{
    protected override SecondaryPattern Pattern => SecondaryPattern.Spitball;
    void Awake() => SetData("Spitball", "Secondary fire", "Lobs a slow, heavy pellet ahead.", ItemType.passiveWeapon);
}
