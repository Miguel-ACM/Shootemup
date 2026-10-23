[UnityEngine.Scripting.Preserve]
public sealed class FlameProjectorItem : AWeaponItem
{
    void Awake(){weaponName="FlameProjector";SetData("Flame Projector","Weapon","Short-range fire applies stacking burn without needing Lighter.",ItemType.weapon);}
    public override void OnBegin(){}
}
