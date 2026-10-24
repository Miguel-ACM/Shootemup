[UnityEngine.Scripting.Preserve]
public sealed class DiscThrowerItem : AWeaponItem
{
    void Awake(){weaponName="DiscThrower";SetData("Disc Thrower","Weapon","Discs hit again on return.",ItemType.weapon);}
    public override void OnBegin(){}
}
