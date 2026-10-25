[UnityEngine.Scripting.Preserve]
public sealed class RailDriverItem : AWeaponItem
{
    void Awake()
    {
        weaponName = "RailDriver";
        SetData("Rail Driver", "Weapon", "Slow-firing, heavy piercing shots.", ItemType.weapon);
    }

    public override void OnBegin()
    {
    }
}
