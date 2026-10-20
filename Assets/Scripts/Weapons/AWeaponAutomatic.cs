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
        KeepFiring();
    }

    // Returns false, and stops the repeating Shoot, once the weapon's button is released.
    // Subclasses must check it before firing: a return inside base.Shoot() only leaves the
    // base method, which used to let one extra volley out after every release.
    protected bool KeepFiring()
    {
        if (weaponInput == WeaponInput.primary)
        {
            if (isStopingPrimary || GameRules.playerDisabled)
            {
                CancelInvoke("Shoot");
                isShootingPrimary = false;
                return false;
            }
        }
        else if (weaponInput == WeaponInput.secondary)
        {
            if (isStopingSecondary || GameRules.playerDisabled)
            {
                CancelInvoke("Shoot");
                isShootingSecondary = false;
                return false;
            }
        }
        else
        {
            if ((isStopingPrimary && isStopingSecondary) || GameRules.playerDisabled)
            {
                CancelInvoke("Shoot");
                // Passive weapons (both inputs) must clear these too, or PrimaryPressed ignores every
                // later press: the AK45 stopped after the first release (e.g. around a shop pickup).
                isShootingPrimary = false;
                isShootingSecondary = false;
                return false;
            }
        }
        return true;
    }

}
