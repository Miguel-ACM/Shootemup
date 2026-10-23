[UnityEngine.Scripting.Preserve]
public sealed class BeamEmitterItem : AWeaponItem
{
    void Awake(){weaponName="BeamEmitter";SetData("Beam Emitter","Weapon","Charged sustained beam. Extra arms add emitters with shared damage. Splitter Core forks its hits.",ItemType.weapon);}
    public override void OnBegin(){}
}
