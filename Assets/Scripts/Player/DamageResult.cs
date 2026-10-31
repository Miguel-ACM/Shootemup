using UnityEngine;

public interface ICombatDamageable
{
    bool CanReceiveDamage { get; }
    DamageResult ReceiveDamage(float amount);
}

public readonly struct DamageResult
{
    public readonly bool Accepted;
    public readonly float Requested, Applied;
    public DamageResult(bool accepted, float requested, float applied)
    {
        Accepted = accepted;
        Requested = requested;
        Applied = Mathf.Max(0, applied);
    }
}
