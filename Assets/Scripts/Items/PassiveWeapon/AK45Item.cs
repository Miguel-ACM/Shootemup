public class AK45Item : APassiveWeaponItem
{

    public AK45Item()
    {
    }

    new void Start()
    {
        base.Start();
    }

    private void Awake()
    {
        weaponeName = "AK45Weapon";
        SetData("AK45", "45 degrees of fun", "A passive 45 degree gun.", AItem.ItemType.passiveWeapon);
    }

    public override void OnBegin() { }

    public override void OnPickup()
    {
        base.OnPickup();
    }
}
