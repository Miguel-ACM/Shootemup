public class ElectricityItem : AWeaponItem
{

    public ElectricityItem()
    {
    }
    new void Start()
    {
        base.Start();
    }

    private void Awake()
    {
        weaponeName = "ElectricityWeapon";
        SetData("Electricity", "ZiP-ZOOM!", "A weapon emits lighting that follows and perforates enemies.", AItem.ItemType.weapon);
    }

    public override void OnBegin() { }

    public override void OnPickup()
    {
        base.OnPickup();
    }
}
