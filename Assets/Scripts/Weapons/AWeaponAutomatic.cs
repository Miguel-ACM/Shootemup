using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class AWeaponAutomatic : AWeapon
{
    float shotClock;
    void Update()
    {
        if(GameRules.playerDisabled || Menu.GameplayPaused || (Time.timeScale<=0 && !WorldTimeStop.PlayerCanAct))return;
        if(!isShootingPrimary && !isShootingSecondary)return;
        if(!KeepFiring())return;
        shotClock-=WorldTimeStop.PlayerDelta*GameRules.playerShootSpeed*shootSpeedModifier*MagnifyingLens.Rate;
        if(shotClock<=0){shotClock=Mathf.Max(0,shotClock+1);Shoot();}
    }
    public override void StopFiring(){base.StopFiring();shotClock=0;}
    public AWeaponAutomatic()
    {
        weaponType = WeaponType.automatic;
    }

    public override void WeaponPressed(bool pressedPrimary)
    {
        bool firing=isShootingPrimary || isShootingSecondary;
        base.WeaponPressed(pressedPrimary);
        if(!firing && (isShootingPrimary || isShootingSecondary))
            shotClock=0; // Fire after all input actions have processed this frame, including Focus release.
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
        if (EffectiveInput == WeaponInput.primary)
        {
            if (isStopingPrimary || GameRules.playerDisabled)
            {
                CancelInvoke("Shoot");
                isShootingPrimary = false;
                return false;
            }
        }
        else if (EffectiveInput == WeaponInput.secondary)
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
