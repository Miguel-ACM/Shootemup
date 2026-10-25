using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : ABullet
{
    Rigidbody2D rigidBody;
    // Shared by all bullets so the scene is searched once, not on every trigger contact.
    static Shipcontroller player;

    void Start()
    {
        ProjectileSolid.Legacy(gameObject,true);
        direction.z = transform.position.z;
        rigidBody = GetComponent<Rigidbody2D>();
        Move();
    }

    // Update is called once per frame
    void Update()
    {
        if (followness != 0)
        {
            GameObject e = Utils.FindClosestWithTag(transform.position, "Enemy", out float sqrDistance);
            if (e != null)
            {
                int factor = 1;
                if (followness < 0)
                    factor = -1;

                if (sqrDistance < Mathf.Abs(followness) * 10)
                {
                    Vector3 d = (e.transform.position - transform.position).normalized;
                    //d.x = Mathf.Cos(d.x);
                    //d.y = Mathf.Sin(d.y);
                    //rigidBody.velocity = Vector3.MoveTowards(transform.position, e.transform.position, 100);//new Vector3(d.x * speed, d.x * speed, direction.z);
                    Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, Vector3.Scale(d, new Vector3(factor, 1, 1)));
                    targetRotation = Utils.ClampRotation(targetRotation, new Vector3(0.1f, 1f, 0));
                    Quaternion nextRotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * followness * 10 * factor);
                    transform.rotation = nextRotation;
                    //new Vector3(transform.up.x * speed, transform.forward.y * speed, direction.z);
                    //Quaternion.RotateTowards(transform.rotation, neededRotation, Time.deltaTime * 10f);

                }
            }

        }
        rigidBody.linearVelocity = transform.up.normalized * speed;
    }

    
    void Move()
    {
        rigidBody.linearVelocity = direction.normalized * speed;//new Vector3(direction.x * speed, direction.y * speed, direction.z);
        transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy")) {
            if (player == null)
                player = GameObject.FindGameObjectWithTag("Player").GetComponent<Shipcontroller>();
            player.PlayHitSound("hit1");
            AEnemy en = collision.gameObject.GetComponent<AEnemy>(); //collision.transform.position
            //Instantiate(Resources.Load<GameObject>("Particles/HitSpark"), collision.transform.position, Quaternion.identity);
            Score.IncreaseScore(Mathf.RoundToInt(damage));
            en.HitAt(collision.ClosestPoint(transform.position),rigidBody!=null ? rigidBody.linearVelocity : (Vector2)transform.up);
            float dealt=CombatUpgrades.Current!=null ? CombatUpgrades.Current.DirectHit(en,damage,0,companionShot) : damage;
            en.damage(dealt);
            if(CombatUpgrades.Current!=null && CombatUpgrades.Current.TryRebound())
            {
                var next=CombatUpgrades.Nearest(transform.position,new HashSet<AEnemy>{en},5);
                if(next!=null)PlayerRound.Spawn(transform.position,(next.transform.position-transform.position).normalized,dealt*.7f,speed,PlayerDelivery.Missile,1);
            }
            Destroy(gameObject);
        }        
    }

    void OnBecameInvisible()
    {
        Destroy(gameObject);
    }
}
