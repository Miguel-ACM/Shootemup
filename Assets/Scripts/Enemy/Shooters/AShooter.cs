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

    private bool movementFiringAllowed = true;
    private Coroutine firingRoutine;
    public Vector3? MovementAimPoint { get; set; }

    public void SetMovementFiringAllowed(bool allowed) { movementFiringAllowed = allowed; }

    protected Vector3 AimPosition(Transform target)
    {
        return MovementAimPoint ?? SpectralDecoy.AimPosition(target != null ? target.position : transform.position + Vector3.down);
    }

    private void BeginShooting()
    {
        if (firingRoutine == null && isActiveAndEnabled) firingRoutine = StartCoroutine(GatedShoot());
    }

    private IEnumerator GatedShoot()
    {
        IEnumerator sequence = Shoot();
        try
        {
            while (true)
            {
                while (!movementFiringAllowed) yield return null;
                if (!sequence.MoveNext()) break;
                yield return sequence.Current;
            }
        }
        finally
        {
            (sequence as System.IDisposable)?.Dispose();
            firingRoutine = null;
        }
    }


    protected virtual void Start()
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
            BeginShooting();
        }
    }

    public virtual void Stop()
    {
        if (firingRoutine != null) StopCoroutine(firingRoutine);
        firingRoutine = null;
    }

    public virtual void OnLeave()
    {
        return;
    }

    public virtual void Trigger()
    {
        if (activation == ShootTrigger.onTrigger)
        {
            BeginShooting();
        }
    }

    public virtual void OnBecameVisible()
    {
        if (activation == ShootTrigger.onVisible)
        {
            BeginShooting();
        }
    }
}
