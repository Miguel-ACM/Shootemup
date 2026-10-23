[UnityEngine.Scripting.Preserve]
public sealed class RailDriverItem : AWeaponItem
{
    void Awake(){weaponName="RailDriver";SetData("Rail Driver","Weapon","Slow cadence, very fast heavy piercing shots. Charge with Capacitor.",ItemType.weapon);}
    public override void OnBegin(){}
}
