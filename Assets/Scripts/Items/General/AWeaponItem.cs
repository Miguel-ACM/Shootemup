using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AWeaponItem : AItem
{

    public string weaponeName;

    public override void OnPickup()
    {
        //s.AddWeaponSecondary(this);
        base.OnPickup();
        s.AddWeaponSecondary(weaponeName);
    }


}
