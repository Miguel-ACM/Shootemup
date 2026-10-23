[UnityEngine.Scripting.Preserve]
public sealed class SwarmRackItem : AWeaponItem
{
    void Awake(){weaponName="SwarmRack";SetData("Swarm Rack","Weapon","Paired seeking missiles chase moving enemies.",ItemType.weapon);}
    public override void OnBegin(){}
}
