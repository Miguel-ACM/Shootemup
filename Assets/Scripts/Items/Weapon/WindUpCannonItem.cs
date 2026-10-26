[UnityEngine.Scripting.Preserve]
public sealed class WindUpCannonItem : AWeaponItem
{
    void Awake()
    {
        weaponName = "WindUpCannon";
        SetData("Charge Blast Cannon", "Hold. Release.", "Charge, then release a lingering blast. Attack speed charges faster.", ItemType.weapon);
    }

    public override void OnBegin()
    {
    }
}
