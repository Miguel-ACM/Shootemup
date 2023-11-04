using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveStraight : AMover
{
    public float[] limits;
    public Vector3 direction = new Vector3(0f, -1f, 0f);
    public float speed = -1f;


    new void Start()
    {
        base.Start();
        limits = Camera.getLimits();
        if (transform.position.x < limits[2])
        {
            direction = new Vector3(1f, 0f, 0f);
        }
        else if (transform.position.x > limits[3])
        {
            direction = new Vector3(-1f, 0f, 0f);
        }
        else if (transform.position.y < 0)
        {
            direction = new Vector3(0f, 1f, 0f);
        }
    }

    public override void Move()
    {
        rigidBody.velocity = direction * speed * GameRules.enemySpeed;
    }


}
