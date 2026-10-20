using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AWeaponItem : AItem
{

    public string weaponName;

    public override void OnPickup()
    {
        //s.AddWeaponSecondary(this);
        base.OnPickup();
        s.AddWeaponSecondary(weaponName);
    }


}
