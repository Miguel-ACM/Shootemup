using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class AWeaponAutomatic : AWeapon
{
    public AWeaponAutomatic()
    {
        weaponType = WeaponType.automatic;
    }

    public override void WeaponPressed(bool pressedPrimary)
    {
        base.WeaponPressed(pressedPrimary);
        if (pressedPrimary && weaponInput == WeaponInput.primary || !pressedPrimary && weaponInput == WeaponInput.secondary || weaponInput == WeaponInput.both)
            InvokeRepeating("Shoot", 0f, 1 / (GameRules.playerShootSpeed * shootSpeedModifier));
    }

    
    public virtual void Shoot()
    {
        if (weaponInput == WeaponInput.primary)
        {
            if (isStopingPrimary || GameRules.playerDisabled)
            {
                CancelInvoke("Shoot");
                isShootingPrimary = false;
                return;
            }
        }
        else if (weaponInput == WeaponInput.secondary)
        {
            if (isStopingSecondary || GameRules.playerDisabled)
            {
                CancelInvoke("Shoot");
                isShootingSecondary = false;
                return;
            }
        }
    }

}
