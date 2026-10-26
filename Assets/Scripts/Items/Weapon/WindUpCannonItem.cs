[UnityEngine.Scripting.Preserve]
public sealed class WindUpCannonItem : AWeaponItem
{
    void Awake()
    {
        weaponName = "WindUpCannon";
        SetData("Wind-up cannon", "Hold. Release.", "Charge, then release a lingering blast. Attack speed charges faster.", ItemType.weapon);
    }

    public override void OnBegin()
    {
    }
}
