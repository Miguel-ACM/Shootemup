[UnityEngine.Scripting.Preserve]
public sealed class BackdraftItem : AWeaponItem
{
    void Awake()
    {
        weaponName = "Backdraft";
        SetData("Rear Flamethrower", "Rear flamethrower", "A wide rear flame cone. Stacking burn.", ItemType.weapon);
    }
    public override void OnBegin() { }
}
