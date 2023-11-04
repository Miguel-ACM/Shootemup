using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AngledShootout : AShooter
{

    public float initialShot = 1.5f;
    public float shootEach = 1.5f;
    public float shotSpeed = 3f;
    public float shotDamage = 3f;
    protected GameObject player;
    public GameObject bullet;
    private float mainAngle = 0f;
    [SerializeField] private int numBullets = 30;
    [SerializeField] private float switchAngle = 3f;


    new public void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }
    // Start is called before the first frame update

    protected override IEnumerator Shoot()
    {
        while (true)
        {
            for (int i = 0; i < numBullets; i++)
            {
                AEnemyBullet createdBullet = Instantiate(bullet, transform.position, transform.rotation).GetComponent<AEnemyBullet>();
                createdBullet.setProperties(new Dictionary<string, float> { { "damage", shotDamage * GameRules.enemyDamage }, { "angle", 360 / numBullets * i + mainAngle }, { "speed", shotSpeed * GameRules.enemyBulletSpeed } });
            }
            if (transform.position.x < 0)
            {
                mainAngle += switchAngle;
            }
            else
            {
                mainAngle -= switchAngle;
            }
            yield return new WaitForSeconds(shootEach / GameRules.enemyShootSpeed);
        }
    }
}
