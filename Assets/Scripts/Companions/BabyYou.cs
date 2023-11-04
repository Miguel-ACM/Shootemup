using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BabyYou : ACompanion
{
    public bool isShootingPrimary = false;
    public bool isStopingPrimary = true;
    bool isShootingDisabled = false;
    Vector3[] spawnPoints = { new Vector3(0,0,0) };    
    private Inputs input;
    [SerializeField] private GameObject bullet;

    public BabyYou()
    {
        type = CompanionType.follower;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

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
            InvokeRepeating("shootPrimary", 0f, 1 / GameRules.playerShootSpeed);
        }
    }

    void PrimaryReleasedCompanion(InputAction.CallbackContext ctx)
    {
        isStopingPrimary = true;
    }

    void shootPrimary()
    {
        if (isStopingPrimary || isShootingDisabled)
        {
            isShootingPrimary = false;
            CancelInvoke("shootPrimary");
            return;
        }

        SoundManager.PlaySound("shot1", 0.1f);
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Bullet b = Instantiate(bullet, spawnPoints[i] + transform.position, Quaternion.identity).GetComponent<Bullet>();
            b.setProperties(new Dictionary<string, float> { { "speed", GameRules.playerShootTravelSpeed }, { "damage", GameRules.playerShootDamage * 0.5f}, { "size", GameRules.playerShootSize * 0.75f }, { "followness", GameRules.playerShootFollowness } });
        }
    }

}
