using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameRules : MonoBehaviour
{
    public static float enemySpeed;
    public static float enemyBulletSpeed; // More is faster
    public static float enemyShootSpeed; // More is faster
    public static float enemyDamage;
    public static float enemyDamageReceived;
    public static float enemyHealth;

    // Player Shooting
    public static bool playerDisabled;
    public static float playerShootSpeed;
    public static int playerShootNumber;
    public static float playerShootDamage;
    public static float playerShootTravelSpeed;
    public static float playerShootSize;
    public static float playerShootFollowness;
    public static float playerMoveSpeed;

    // Pickables
    public static float luck;
    public static float itemLuck;
    public static int morePickables;
    public static float scoreMultiplier;
    public static int numShopItems;
    public static float healMultiplier;
    public static float manaHealMultiplier;
    public static float pickDistance;

    // Companions
    public static int followCompanionDelay;
    public static float orbitTime;
    public static float orbitDistance;

    static GameRules()
    {
        ResetRun();
    }

    // Statics survive scene loads in a build, so every new run has to start from these defaults
    // or it inherits the last run's items and the frozen ship from Shipcontroller.Die().
    public static void ResetRun()
    {
        enemySpeed = 1f;
        enemyBulletSpeed = 1f;
        enemyShootSpeed = 1f;
        enemyDamage = 1f;
        enemyDamageReceived = 1f;
        enemyHealth = 1f;

        playerDisabled = false;
        playerShootSpeed = 10f;
        playerShootNumber = 2;
        playerShootDamage = 2f;
        playerShootTravelSpeed = 16f;
        playerShootSize = 1.5f;
        playerShootFollowness = 0f;
        playerMoveSpeed = 8f;

        luck = 0f;
        itemLuck = 1f;
        morePickables = 0;
        scoreMultiplier = 1f;
        numShopItems = 3;
        healMultiplier = 1f;
        manaHealMultiplier = 1f;
        pickDistance = 1f;

        followCompanionDelay = 8;
        orbitTime = 5f;
        orbitDistance = 1f;
    }

    // Resets everything a run accumulates: player stats, score and the shop's item pools.
    public static void StartNewRun(RunDifficulty difficulty = RunDifficulty.Hard)
    {
        CombatDecision.End();
        ResetRun();
        Difficulty.Select(difficulty);
        enemyDamage *= Difficulty.Damage;
        Score.ResetRun();
        RunCoins.Reset();
        ItemPool.ResetRun();
    }
}
