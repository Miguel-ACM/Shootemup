[UnityEngine.Scripting.Preserve]
public sealed class FlameProjectorItem : AWeaponItem
{
    void Awake()
    {
        weaponName = "FlameProjector";
        SetData("Flame Projector", "Weapon", "Short-range, stacking burn.", ItemType.weapon);
    }

    public override void OnBegin()
    {
    }
}
