[UnityEngine.Scripting.Preserve]
public sealed class ScatterCannonItem : AWeaponItem
{
    void Awake(){weaponName="ScatterCannon";SetData("Scatter Cannon","Weapon","A close fan of five metal rounds from each muzzle.",ItemType.weapon);}
    public override void OnBegin(){}
}
