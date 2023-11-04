using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AShooter : MonoBehaviour
{

    protected AMover mover;
    protected AEnemy enemy;
    public enum ShootTrigger
    {
        None,
        onArrive,
        onVisible,
        onTrigger
    }
    public ShootTrigger activation = ShootTrigger.None;


    public void Start()
    {
        mover = GetComponent<AMover>();
        enemy = GetComponent<AEnemy>();
    }
    // Start is called before the first frame update

    protected abstract IEnumerator Shoot();

    public virtual void OnArrive()
    {
        if (activation == ShootTrigger.onArrive)
        {
            StartCoroutine("Shoot");
        }
    }

    public virtual void Stop()
    {
        StopCoroutine("Shoot");
    }

    public virtual void OnLeave()
    {
        return;
    }

    public virtual void Trigger()
    {
        if (activation == ShootTrigger.onTrigger)
        {
            StartCoroutine("Shoot");
        }
    }

    public virtual void OnBecameVisible()
    {
        if (activation == ShootTrigger.onVisible)
        {
            StartCoroutine("Shoot");
        }
    }
}
