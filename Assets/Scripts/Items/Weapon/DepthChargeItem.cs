[UnityEngine.Scripting.Preserve]
public sealed class DepthChargeItem : AWeaponItem
{
    void Awake()
    {
        weaponName = "DepthCharge";
        SetData("Depth Charge", "Rear shell rack", "Drop shells behind you. They brake, then burst into a rear fan.", ItemType.weapon);
    }

    public override void OnBegin()
    {
    }
}
