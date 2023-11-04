using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AMover : MonoBehaviour
{

    protected Rigidbody2D rigidBody;
    protected AShooter shooter;
    protected bool movementEnabled = true;
    // Start is called before the first frame update
    protected void Start()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        shooter = GetComponent<AShooter>();
    }

    // Update is called once per frame
    void Update()
    {
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
