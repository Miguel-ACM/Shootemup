using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FocusingSingleBurst : AShooter
{
    [SerializeField] private float initialShot = 1.5f;
    [SerializeField] private float shootEach = 128f;
    [SerializeField] private float shotSpeed = 3f;
    [SerializeField] private float shotDamage = 3f;
    [SerializeField] private float microShootEach = 5f;
    protected GameObject player;
    int numCurShot = 0;
    [SerializeField] private int numBullets = 300;
    [SerializeField] private GameObject bullet;
    [SerializeField] private float[] bulletSpread = {35f, 1f};
    [SerializeField] private AnimationCurve bulletSpreadCurve;



    new public void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }
    // Start is called before the first frame update

    protected override IEnumerator Shoot()
    {
        yield return new WaitForSeconds(initialShot);
        float currentBulletSpread;
        float bulletSpreadDistance = bulletSpread[0] - bulletSpread[1];
        while (true)
        {
            Vector3 playerPos = player.transform.position;
            float angle = Utils.GetAngleBetweenVectors(transform.position, playerPos);

            AEnemyBullet createdBullet = Instantiate(bullet, transform.position, transform.rotation).GetComponent<AEnemyBullet>();
            currentBulletSpread = (bulletSpreadCurve.Evaluate((float) numCurShot / numBullets)) * bulletSpreadDistance + bulletSpread[1];
            createdBullet.setProperties(new Dictionary<string, float> { { "damage", shotDamage * GameRules.enemyDamage }, { "angle", Utils.RandomVariateAngle(angle, currentBulletSpread) }, { "speed", shotSpeed * GameRules.enemyBulletSpeed } });
            numCurShot += 1;
            if (numCurShot == numBullets)
            {
                numCurShot = 0;
                currentBulletSpread = bulletSpread[0];
                yield return new WaitForSeconds(shootEach / GameRules.enemyShootSpeed);
            }
            else
            {
                yield return new WaitForSeconds(microShootEach);
            }
        }
        // Code to execute after the delay
    }

    override public void Stop()
    {
        numCurShot = 0;
        base.Stop();
    }
}
