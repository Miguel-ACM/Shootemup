[UnityEngine.Scripting.Preserve]
public sealed class SwarmRackItem : AWeaponItem
{
    void Awake()
    {
        weaponName = "SwarmRack";
        SetData("Swarm Rack", "Weapon", "Twin homing missiles.", ItemType.weapon);
    }

    public override void OnBegin()
    {
    }
}
