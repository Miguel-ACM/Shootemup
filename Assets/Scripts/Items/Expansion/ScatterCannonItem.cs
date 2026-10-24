[UnityEngine.Scripting.Preserve]
public sealed class ScatterCannonItem : AWeaponItem
{
    void Awake(){weaponName="ScatterCannon";SetData("Scatter Cannon","Weapon","Five-shot spread.",ItemType.weapon);}
    public override void OnBegin(){}
}
