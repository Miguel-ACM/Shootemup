public class AK45Item : APassiveWeaponItem
{
    private void Awake()
    {
        weaponName = "AK45Weapon";
        SetData("AK45", "45 degrees of fun", "Side-firing gun.", AItem.ItemType.passiveWeapon);
    }

    public override void OnBegin() { }
}
