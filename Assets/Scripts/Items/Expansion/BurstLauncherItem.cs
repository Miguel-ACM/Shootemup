[UnityEngine.Scripting.Preserve]
public sealed class BurstLauncherItem : AWeaponItem
{
    void Awake(){weaponName="BurstLauncher";SetData("Burst Launcher","Weapon","Heavy shells burst on impact or after stopping.",ItemType.weapon);}
    public override void OnBegin(){}
}
