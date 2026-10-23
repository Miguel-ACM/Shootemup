[UnityEngine.Scripting.Preserve]
public sealed class DiscThrowerItem : AWeaponItem
{
    void Awake(){weaponName="DiscThrower";SetData("Disc Thrower","Weapon","Returning discs hit again on their return pass.",ItemType.weapon);}
    public override void OnBegin(){}
}
