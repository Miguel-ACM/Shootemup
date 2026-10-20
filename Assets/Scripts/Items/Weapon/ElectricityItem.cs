public class ElectricityItem : AWeaponItem
{
    private void Awake()
    {
        weaponeName = "ElectricityWeapon";
        SetData("Electricity", "ZiP-ZOOM!", "A weapon emits lighting that follows and perforates enemies.", AItem.ItemType.weapon);
    }

    public override void OnBegin() { }
}
