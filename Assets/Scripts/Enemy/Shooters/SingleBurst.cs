using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SingleBurst : AShooter
{
    [SerializeField] private float initialShot = 1.5f;
    [SerializeField] private float shootEach = 1.5f;
    [SerializeField] private float shotSpeed = 3f;
    [SerializeField] private float shotDamage = 3f;
    [SerializeField] private float microShootEach = 0.1f;
    protected GameObject player;
    int numCurShot = 0;
    [SerializeField] private int numBullets = 3;
    [SerializeField] private GameObject bullet;
    [SerializeField] private float bulletSpread = 10f;


    new public void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }
    // Start is called before the first frame update

    protected override IEnumerator Shoot()
    {
        yield return new WaitForSeconds(initialShot);
        while (true)
        {
            Vector3 playerPos = player.transform.position;
            float angle = Utils.GetAngleBetweenVectors(transform.position, playerPos);

            AEnemyBullet createdBullet = Instantiate(bullet, transform.position, transform.rotation).GetComponent<AEnemyBullet>();
            createdBullet.setProperties(new Dictionary<string, float> { { "damage", shotDamage * GameRules.enemyDamage }, { "angle", Utils.RandomVariateAngle(angle, bulletSpread) }, { "speed", shotSpeed * GameRules.enemyBulletSpeed } });
            numCurShot += 1;
            if (numCurShot == numBullets)
            {
                numCurShot = 0;
                yield return new WaitForSeconds(shootEach / GameRules.enemyShootSpeed);
            }
            else
            {
                yield return new WaitForSeconds(microShootEach);
            }
        }
        // Code to execute after the delay
    }

    new public void Stop()
    {
        numCurShot = 0;
        base.Stop();
    }

}
