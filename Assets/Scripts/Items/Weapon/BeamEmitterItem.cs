[UnityEngine.Scripting.Preserve]
public sealed class BeamEmitterItem : AWeaponItem
{
    void Awake()
    {
        weaponName = "BeamEmitter";
        SetData("Beam Emitter", "Weapon", "Charged, sustained beam.", ItemType.weapon);
    }

    public override void OnBegin()
    {
    }
}
