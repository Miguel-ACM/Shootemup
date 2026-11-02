[UnityEngine.Scripting.Preserve]
public sealed class SaltShaker : ASecondaryItem
{
    protected override SecondaryPattern Pattern => SecondaryPattern.Salt;
    void Awake() { SetData("Salt Shaker", "Secondary fire", "A weak rear fan. Focus gathers the grains.", ItemType.passiveWeapon); }
}
