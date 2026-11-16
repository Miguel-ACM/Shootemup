using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AMover : MonoBehaviour
{

    protected Rigidbody2D rigidBody;
    AEnemy movementOwner;
    protected float ChillMovement => movementOwner != null ? movementOwner.ChillMovement : 1;
    protected AShooter shooter;
    protected bool movementEnabled = true;
    // Start is called before the first frame update
    protected virtual void Start()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        movementOwner = GetComponent<AEnemy>();
        shooter = GetComponent<AShooter>();
    }

    // Update is called once per frame
    void Update()
    {
        if (FrozenHull.Holds(gameObject))
        {
            if (rigidBody != null)
                rigidBody.linearVelocity = Vector2.zero;
            return;
        }
        if (movementEnabled)
            Move();
    }

    public abstract void Move();

    public virtual void OnShootEnd()
    {
        return;
    }

    public virtual void OnShootStart()
    {
        return;
    }

    public void SetMove(bool enable)
    {
        movementEnabled = enable;
    }


}
