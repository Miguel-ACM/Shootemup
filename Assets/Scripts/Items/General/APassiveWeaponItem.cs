using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class APassiveWeaponItem : AItem
{

    public string weaponName;

    public override void OnPickup()
    {
        base.OnPickup();
        s.AddPassiveWeapon(weaponName);
    }


}
