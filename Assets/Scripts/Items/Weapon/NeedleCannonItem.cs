[UnityEngine.Scripting.Preserve]
public sealed class NeedleCannonItem : AWeaponItem
{
    void Awake()
    {
        weaponName = "NeedleCannon";
        SetData("Needle Cannon", "Weapon", "Fast shots pierce one enemy.", ItemType.weapon);
    }

    public override void OnBegin()
    {
    }
}
