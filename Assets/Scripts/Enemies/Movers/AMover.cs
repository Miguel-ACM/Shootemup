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
    Vector2 kick, appliedKick;
    float nextKick;

    public void Kick(Vector2 displacement)
    {
        if (Time.time < nextKick || FrozenHull.Holds(gameObject)) return;
        nextKick = Time.time + .085f;
        kick = Vector2.ClampMagnitude(kick + displacement, .65f);
    }
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
        if (!movementEnabled) return;
        if (appliedKick != Vector2.zero)
            transform.position -= (Vector3)appliedKick;
        Move();
        // Layer the impact over the authored route so a formation keeps its original timing.
        if (kick != Vector2.zero)
            kick = Vector2.MoveTowards(kick, Vector2.zero, 2.25f * Time.deltaTime);
        appliedKick = kick;
        if (appliedKick != Vector2.zero)
            transform.position += (Vector3)appliedKick;
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
