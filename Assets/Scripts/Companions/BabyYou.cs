using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BabyYou : ACompanion
{
    public bool isShootingPrimary = false;
    public bool isStopingPrimary = true;
    bool isShootingDisabled = false;
    Vector3[] spawnPoints = { new Vector3(0, 0, 0) };
    [SerializeField] private GameObject bullet;

    public BabyYou()
    {
        type = CompanionType.follower;
    }

    void Awake()
    {
        _ = GameInput.Actions;

        GameInput.PrimaryPressed += PrimaryPressedCompanion;
        GameInput.PrimaryReleased += PrimaryReleasedCompanion;
    }

    void Start()
    {
        var owner = GetComponentInParent<Shipcontroller>();
        if (owner == null) owner = FindAnyObjectByType<Shipcontroller>();
        if (owner == null) return;
        var miniature = ShipFrames.BuildDisplay(owner, ShipFrames.Current);
        if (miniature == null) return;
        var original = GetComponent<PSXObjectVisual>();
        if (original != null) original.Hide();
        var sprite = GetComponent<SpriteRenderer>();
        if (sprite != null) sprite.enabled = false;
        miniature.transform.localScale *= .48f;
        miniature.transform.SetParent(transform, true);
        miniature.transform.localPosition = Vector3.zero;
        miniature.transform.localRotation = Quaternion.identity;
        foreach (var renderer in miniature.GetComponentsInChildren<MeshRenderer>())
            renderer.sortingLayerName = "Player";
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
        if (isStopingPrimary || isShootingDisabled || GameRules.playerDisabled)
        {
            isShootingPrimary = false;
            CancelInvoke("shootPrimary");
            return;
        }

        SoundManager.PlaySound("shotCompanion", 0.1f);
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Bullet b = Instantiate(bullet, spawnPoints[i] + transform.position, Quaternion.identity).GetComponent<Bullet>();
            b.babyShot = true;
            b.setProperties(new Dictionary<string, float> { { "companion", 1f }, { "speed", GameRules.playerShootTravelSpeed }, { "damage", GameRules.playerShootDamage * 0.5f }, { "size", GameRules.playerShootSize * 0.75f }, { "followness", GameRules.playerShootFollowness } });
            b.direction = CombatTargets.MarkedDirection(b.transform.position, CombatTargets.MarkedInRange(b.transform.position, 16));
        }
    }

    // Inputs instances stay enabled after their scene unloads, and their callbacks then run
    // against this destroyed object, so release it with the object.
    void OnDestroy()
    {
        GameInput.PrimaryPressed -= PrimaryPressedCompanion;
        GameInput.PrimaryReleased -= PrimaryReleasedCompanion;
    }
}
