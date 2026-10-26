[UnityEngine.Scripting.Preserve]
public sealed class BeamEmitterItem : AWeaponItem
{
    void Awake()
    {
        weaponName = "BeamEmitter";
        SetData("Beam Emitter", "Weapon", "Hold for a continuous beam. Telekinesis bends it.", ItemType.weapon);
    }

    public override void OnBegin()
    {
    }
}
