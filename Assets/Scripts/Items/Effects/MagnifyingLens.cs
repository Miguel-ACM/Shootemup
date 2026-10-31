using UnityEngine;
public sealed class MagnifyingLens : MonoBehaviour
{
    public static MagnifyingLens Current
    {
        get; private set;
    }

    float charge;
    public float Charge => GameInput.Snapshot.Focus && !GameRules.playerDisabled ? charge : 0;
    public static float Rate => Current != null ? Mathf.Lerp(1, .5f, Current.Charge) : 1;
    public static float Power => Current != null ? Mathf.Lerp(1, 3, Current.Charge) : 1;
    void Awake()
    {
        Current = this;
        _ = GameInput.Actions;
    }

    void Update()
    {
        charge = GameInput.Snapshot.Focus && !GameRules.playerDisabled ? Mathf.Min(1, charge + GameClock.PlayerDelta / 1.5f) : 0;
    }

    void OnDestroy()
    {
        if (Current == this)
            Current = null;
    }

    public static void Enlarge(ABullet bullet)
    {
        if (bullet == null || bullet.companionShot || Current == null)
            return;
        bullet.damage *= Power;
        bullet.transform.localScale *= Mathf.Lerp(1, 1.6f, Current.Charge);
    }
}
