[UnityEngine.Scripting.Preserve]
public sealed class NeedleCannonItem : AWeaponItem
{
    void Awake(){weaponName="NeedleCannon";SetData("Needle Cannon","Weapon","Fast elongated rounds pierce one target.",ItemType.weapon);}
    public override void OnBegin(){}
}
