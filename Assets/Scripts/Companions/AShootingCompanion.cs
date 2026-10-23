using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AShootingCompanion : ACompanion
{
    public bool isShootingPrimary = false;
    public bool isStopingPrimary = true;
    bool isShootingDisabled = false;
    Vector3[] spawnPoints = { new Vector3(0,0,0) };    
    private Inputs input;
    [SerializeField] private GameObject bullet;
    [SerializeField] private float shootSpeed = 8f;
    [SerializeField] private float shootTravelSpeed = 5f;
    [SerializeField] private float shootDamage = .2f;
    [SerializeField] private float shootSize = 2f;
    [SerializeField] private float shootFollowness = 0;

    void Awake()
    {
        input = new Inputs();
        input.Enable();

        input.PlayerControls.FirePrimary.started += PrimaryPressedCompanion;
        input.PlayerControls.FirePrimary.canceled += PrimaryReleasedCompanion;
    }

    void PrimaryPressedCompanion(InputAction.CallbackContext ctx)
    {
        isStopingPrimary = false;
        if (!isShootingPrimary && !isShootingDisabled)
        {
            isShootingPrimary = true;
            InvokeRepeating("shootPrimary", 0f, 1 / shootSpeed);
        }
    }

    void PrimaryReleasedCompanion(InputAction.CallbackContext ctx)
    {
        isStopingPrimary = true;
    }

    void shootPrimary()
    {
        if (isStopingPrimary || isShootingDisabled || GameRules.playerDisabled)
        {
            isShootingPrimary = false;
            CancelInvoke("shootPrimary");
            return;
        }

        SoundManager.PlaySound("shot1", 0.1f);
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Bullet b = Instantiate(bullet, spawnPoints[i] + transform.position, Quaternion.identity).GetComponent<Bullet>();
            b.setProperties(new Dictionary<string, float> { { "companion", 1f }, { "speed", shootTravelSpeed }, { "damage", shootDamage }, { "size", shootSize }, { "followness", shootFollowness } });
        }
        //Bullet b = Instantiate(bulletPrimary, spawnPoints[currBulletOutIdx] + transform.position, Quaternion.identity).GetComponent<Bullet>();
        //b.setProperties(new Dictionary<string, float> { { "companion", 1f }, { "speed", shootTravelSpeed }, { "damage", shootDamage }, { "size", shootSize }, { "followness", shootFollowness } });
        //NextShootIdx();
    }

    // Inputs instances stay enabled after their scene unloads, and their callbacks then run
    // against this destroyed object, so release it with the object.
    void OnDestroy()
    {
        input.Disable();
        input.Dispose();
    }
}
