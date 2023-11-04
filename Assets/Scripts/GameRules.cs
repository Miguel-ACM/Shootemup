using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameRules : MonoBehaviour
{
    public static float enemySpeed = 1f;
    public static float enemyBulletSpeed = 1f; // More is faster
    public static float enemyShootSpeed = 1f; // More is faster
    public static float enemyDamage = 1f;
    public static float enemyDamageReceived = 1f;
    public static float enemyHealth = 1f;

    // Player Shooting
    public static bool playerDisabled = false;
    public static float playerShootSpeed = 10f;
    public static int playerShootNumber = 2;
    public static float playerShootDamage = 2f;
    public static float playerShootTravelSpeed = 16f;
    public static float playerShootSize = 1.5f;
    public static float playerShootFollowness = 0f;
    public static float playerMoveSpeed = 8f;

    // Pickables
    public static float luck = 0f;
    public static float itemLuck = 1f;
    public static int morePickables = 0;
    public static float scoreMultiplier = 1f;
    public static int numShopItems = 3;
    public static float healMultiplier = 1f;
    public static float manaHealMultiplier = 1f;
    public static float pickDistance = 1f;

    // Companions
    public static int followCompanionDelay = 8;
    public static float orbitTime = 5f;
    public static float orbitDistance = 1f;


}
