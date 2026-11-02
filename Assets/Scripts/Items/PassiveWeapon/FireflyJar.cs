[UnityEngine.Scripting.Preserve]
public sealed class FireflyJar : ASecondaryItem
{
    protected override SecondaryPattern Pattern => SecondaryPattern.Firefly;
    void Awake() { SetData("Firefly Jar", "Secondary fire", "Small seeking sparks alongside your weapon.", ItemType.passiveWeapon); }
}
