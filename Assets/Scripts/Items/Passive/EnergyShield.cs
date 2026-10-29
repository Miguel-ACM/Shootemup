using UnityEngine;

// A shield around the last hit: it absorbs the hit that would end the run, then recharges over a minute of combat.
[UnityEngine.Scripting.Preserve]
public sealed class EnergyShield : AItem
{
    void Awake()
    {
        SetData("Energy Shield", "Last line", "Shields your last hit. Recharges in 1 minute.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        if (s.GetComponent<EnergyShieldCell>() == null)
            s.gameObject.AddComponent<EnergyShieldCell>();
        base.OnPickup();
    }
}

public sealed class EnergyShieldCell : MonoBehaviour
{
    public const float RechargeSeconds = 60;
    static readonly Color Field = new Color(.45f, .85f, 1f);
    public float Charge
    {
        get; private set;
    } = 1;
    public bool Ready => Charge >= 1;
    // 1 the moment the shield breaks or comes back, fading quickly; the HUD flashes with it.
    public float Flash
    {
        get; private set;
    }

    public bool Absorb()
    {
        if (!Ready)
            return false;
        Charge = 0;
        Flash = 1;
        PSXFireballs.Shockwave(transform.position, 1.8f, .45f, Field, 2, .5f);
        PSXImpactEffects.Burst(transform.position, Field);
        ItemSound.Play("shatter", .22f);
        return true;
    }

    void Update()
    {
        Flash = Mathf.Max(0, Flash - Time.unscaledDeltaTime * 2.5f);
        if (Ready)
            return;
        // Only active play counts: shops, route choices, transitions and pauses do not recharge it.
        if (CombatDecision.Active || CloudTransition.Busy || GameRules.playerDisabled || GameOverState.Active)
            return;
        Charge = Mathf.Min(1, Charge + Time.deltaTime / RechargeSeconds);
        if (Ready)
        {
            Flash = 1;
            PSXFireballs.Shockwave(transform.position, 1.2f, .35f, Field, 1, .6f);
            ItemSound.Play("ready", .18f);
        }
    }
}
