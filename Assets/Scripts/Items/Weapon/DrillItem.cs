[UnityEngine.Scripting.Preserve]
public sealed class DrillItem : AWeaponItem
{
    void Awake()
    {
        weaponName = "Drill";
        SetData("Spiral Drill", "Weapon", "Hold to drill. Slows bullets. Grows as it grinds.", ItemType.weapon);
    }

    public override void OnBegin()
    {
    }
}
