[UnityEngine.Scripting.Preserve]
public sealed class WindUpCannonItem : AWeaponItem
{
    void Awake(){weaponName="WindUpCannon";SetData("Wind-up cannon","Hold. Release.","Three charge levels. Charged shots pierce enemies.",ItemType.weapon);}
    public override void OnBegin(){}
}
