[UnityEngine.Scripting.Preserve]
public sealed class BurstLauncherItem : AWeaponItem
{
    void Awake(){weaponName="BurstLauncher";SetData("Burst Launcher","Weapon","Exploding shells.",ItemType.weapon);}
    public override void OnBegin(){}
}
