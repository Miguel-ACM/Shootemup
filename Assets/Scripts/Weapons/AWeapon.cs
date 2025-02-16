using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class AWeapon : MonoBehaviour
{
    Inputs input;

    public enum WeaponType
    {
        automatic,
        charge,
        hold
    }

    public enum WeaponInput
    {
        primary,
        secondary,
        both
    }

    public bool isStopingPrimary = true;
    public bool isShootingPrimary = false;
    public bool isStopingSecondary = true;
    public bool isShootingSecondary = false;
    public bool stillShooting = false;

    protected WeaponInput weaponInput;
    protected WeaponType weaponType;
    protected float shootSpeedModifier = 1f;

    protected Vector3[] spawnPoints = null;
    float spreadInclination = 1.5f;
    float spreadOffset = 1f;
    // Start is called before the first frame update
    public void Start()
    {
        updateSpawnPoints();
        input = new Inputs();


        input.PlayerControls.FirePrimary.started += PrimaryPressed;
        input.PlayerControls.FirePrimary.canceled += PrimaryReleased;

        input.PlayerControls.FireSecondary.started += SecondaryPressed;
        input.PlayerControls.FireSecondary.canceled += SecondaryReleased;

        input.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void SetWeaponInput(WeaponInput wi)
    {
        weaponInput = wi;
    }



    void PrimaryPressed(InputAction.CallbackContext ctx)
    {
        Debug.Log("Primary Pressed");
        isStopingPrimary = false;
        isStopingSecondary = true;
        //isShootingSecondary = false;
        if (!isShootingPrimary && !GameRules.playerDisabled)
        {
            WeaponPressed(true);
            // InvokeRepeating("shootPrimary", 0f, 1 / GameRules.playerShootSpeed);
        }
    }

    void SecondaryPressed(InputAction.CallbackContext ctx)
    {
        isStopingSecondary = false;
        isStopingPrimary = true;
        //isShootingPrimary = false;
        if (!isShootingSecondary && !GameRules.playerDisabled)
        {
            WeaponPressed(false);
            // InvokeRepeating("shootPrimary", 0f, 1 / GameRules.playerShootSpeed);
        }
    }

    public virtual void WeaponPressed(bool pressedPrimary)
    {
        if (weaponInput == WeaponInput.both)
        {
            if ((pressedPrimary && isShootingSecondary) || (!pressedPrimary && isShootingPrimary))
            {
                return;
            }
        }
        Debug.Log(pressedPrimary.ToString() + " " + isStopingPrimary.ToString());
        if (isShootingPrimary && pressedPrimary ||isShootingSecondary && !pressedPrimary || IsInvoking("Shoot") || pressedPrimary && weaponInput == WeaponInput.secondary || !pressedPrimary && weaponInput == WeaponInput.primary)
        {
            return;
        }
        if (pressedPrimary)
            isShootingPrimary = true;
        else
            isShootingSecondary = true;
    }
    void PrimaryReleased(InputAction.CallbackContext ctx)
    {
        isStopingPrimary = true;
    }

    void SecondaryReleased(InputAction.CallbackContext ctx)
    {
        isStopingSecondary = true;
    }

    public virtual void updateSpawnPoints()
    {
        float spread = 0.35f;
        if (GameRules.playerShootNumber > 7)
        {
            spread = 2.45f / GameRules.playerShootNumber;
        }

        Vector3[] auxSpawnPoints = new Vector3[GameRules.playerShootNumber];
        float currentSpread = -(spread / 2) * (GameRules.playerShootNumber - 1);
        for (int i = 0; i < GameRules.playerShootNumber; i++)
        {
            auxSpawnPoints[i] = new Vector3(currentSpread, -Mathf.Abs(currentSpread * spreadInclination) + spreadOffset, 0);
            currentSpread += spread;
        }

        spawnPoints = auxSpawnPoints;

        /*
        spawnPoints = new Vector3[shootNumber];
        int mid = (shootNumber - 1) / 2;
        if (shootNumber > 1)
        {
            bool startedDown = false;
            spawnPoints[0] = auxSpawnPoints[mid];
            if (shootNumber % 2 == 0)
            {
                mid += 1;
            }
            else
            {
                mid -= 1;
                startedDown = true;
            }

            spawnPoints[1] = auxSpawnPoints[mid];


            for (int i = 2; i < shootNumber; i++) {
                if (mid < (shootNumber - 1) / 2)
                {
                    mid += ((shootNumber - 1) / 2 - mid) * 2;
                    if (!startedDown)
                        mid += 1;
                }
                else
                {
                    mid -= ((shootNumber - 1) / 2 - mid) * 2;
                    if (startedDown)
                        mid -= 1;
                }

                spawnPoints[i] = auxSpawnPoints[mid];
            }

            currBulletOutIdx = shootNumber / 2;
        }*/

    }
}
