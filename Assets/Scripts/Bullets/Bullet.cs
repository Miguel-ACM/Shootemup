using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : ABullet
{
    Rigidbody2D rigidBody;

    void Start()
    {
        direction.z = transform.position.z;
        rigidBody = GetComponent<Rigidbody2D>();
        Move();
    }

    // Update is called once per frame
    void Update()
    {
        if (followness != 0)
        {
            GameObject e = Utils.FindClosestWithTag(transform.position, "Enemy", out float distance);
            if (e != null)
            {
                int factor = 1;
                if (followness < 0)
                    factor = -1;

                if (distance < Mathf.Abs(followness) * 10)
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
        rigidBody.velocity = transform.up.normalized * speed;
    }

    
    void Move()
    {
        rigidBody.velocity = direction.normalized * speed;//new Vector3(direction.x * speed, direction.y * speed, direction.z);
        transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        Shipcontroller sc = GameObject.FindGameObjectWithTag("Player").GetComponent<Shipcontroller>();
        if (collision.gameObject.tag == "Enemy") {
            sc.PlayHitSound("hit1");
            AEnemy en = collision.gameObject.GetComponent<AEnemy>(); //collision.transform.position
            //Instantiate(Resources.Load<GameObject>("Particles/HitSpark"), collision.transform.position, Quaternion.identity);
            Score.IncreaseScore(Mathf.RoundToInt(damage));
            en.damage(damage);
            Destroy(gameObject);
        }        
    }


    void OnBecameInvisible()
    {
        Destroy(gameObject);
    }
}
