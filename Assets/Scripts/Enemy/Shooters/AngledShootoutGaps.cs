using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AngledShootoutGaps : AShooter
{
    public float initialShot = 1.5f;
    public float shootEach = 1.5f;
    public float shotSpeed = 3f;
    public float shotDamage = 3f;
    public bool disabled = true;
    public GameObject bullet;
    private float mainAngle = 0f;
    [SerializeField] private int numBullets = 30;


    protected override IEnumerator Shoot()
    {
        while (true)
        {
            for (int i = 0; i < numBullets; i++)
            {
                float angle = 60 / (numBullets - 1) * i + mainAngle;
                AEnemyBullet createdBullet = Instantiate(bullet, transform.position, transform.rotation).GetComponent<AEnemyBullet>();
                createdBullet.setProperties(new Dictionary<string, float> { { "damage", shotDamage * GameRules.enemyDamage }, { "angle", angle }, { "speed", shotSpeed * GameRules.enemyBulletSpeed } });
                createdBullet = Instantiate(bullet, transform.position, transform.rotation).GetComponent<AEnemyBullet>();
                createdBullet.setProperties(new Dictionary<string, float> { { "damage", shotDamage * GameRules.enemyDamage }, { "angle", angle + 120 }, { "speed", shotSpeed * GameRules.enemyBulletSpeed } });
                createdBullet = Instantiate(bullet, transform.position, transform.rotation).GetComponent<AEnemyBullet>();
                createdBullet.setProperties(new Dictionary<string, float> { { "damage", shotDamage * GameRules.enemyDamage }, { "angle", angle + 240 }, { "speed", shotSpeed * GameRules.enemyBulletSpeed } });
            }
            mainAngle += 60;
            yield return new WaitForSeconds(shootEach / GameRules.enemyShootSpeed);
        }
    }


}
