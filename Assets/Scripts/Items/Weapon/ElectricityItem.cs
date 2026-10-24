public class ElectricityItem : AWeaponItem
{
    private void Awake()
    {
        weaponName = "ElectricityWeapon";
        SetData("Electricity", "ZiP-ZOOM!", "Seeking, piercing lightning.", AItem.ItemType.weapon);
    }

    public override void OnBegin() { }
}
