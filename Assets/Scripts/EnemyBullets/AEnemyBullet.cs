using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class AEnemyBullet : MonoBehaviour
{
    Vector3 direction;
    public float damage;
    public float speed;
    Rigidbody2D rigidBody;
    protected bool moveOnUpdate = false;
    protected bool dieOnTrigger = true;

    protected virtual void Start()
    {
        rigidBody = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        Move();
    }

    protected virtual void Move()
    {
        rigidBody.velocity = direction * speed;
    }

    public void setProperties(Dictionary<string, float> stats)
    {
        damage = Utils.GetValueOrDefault(stats, "damage", 1f);
        if (stats.ContainsKey("angle")) {
            float angle = Utils.GetValueOrDefault(stats, "angle", 0f);
            direction = Utils.GetVelocityAngle(angle);
        } else
        {
            float xdir = Utils.GetValueOrDefault(stats, "dirx", 0f);
            float ydir = Utils.GetValueOrDefault(stats, "diry", -1f);
            Vector3 player_pos = new Vector3(xdir, ydir);
            direction = Utils.GetVelocityBetweenVectors(transform.position, player_pos);
        }
        speed = Utils.GetValueOrDefault(stats, "speed", 1f);
    }

    public float getDamage()
    {
        return damage;
    }

    public void OnTrigger()
    {
        if (dieOnTrigger)
        {
            Destroy(gameObject);
        }
    }

    void OnBecameInvisible()
    {
        Destroy(gameObject);
    }
}