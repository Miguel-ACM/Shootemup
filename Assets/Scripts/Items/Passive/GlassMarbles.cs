using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class GlassMarbles : AItem
{
    void Awake()
    {
        SetData("Glass Marbles", "Keep them rolling", "Shots grow with your speed.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Marbles);
        if (s.GetComponent<MarbleGauge>() == null)
            s.gameObject.AddComponent<MarbleGauge>();
        base.OnPickup();
    }
}

// Measures how fast the ship is actually flying, so shot size follows real movement
// (including Underwater drift) rather than stick input.
public sealed class MarbleGauge : MonoBehaviour
{
    static MarbleGauge current;
    Vector3 previous;
    float pace;

    // 1 while still, up to 1.6 at full speed (+.3 per extra copy); Focus shrinks shots by 30%.
    public static float Scale
    {
        get
        {
            int copies = CombatUpgrades.Current != null ? CombatUpgrades.Current.Count(UpgradeKind.Marbles) : 0;
            if (copies == 0 || current == null)
                return 1;
            float grown = Mathf.Lerp(1, 1.6f + .3f * (copies - 1), current.pace);
            return GameInput.Snapshot.Focus ? grown * .7f : grown;
        }
    }

    void OnEnable()
    {
        current = this;
        previous = transform.position;
    }

    void OnDisable()
    {
        if (current == this)
            current = null;
    }

    void LateUpdate()
    {
        float dt = GameClock.PlayerDelta;
        if (dt <= 0)
            return;
        float full = Mathf.Max(.1f, GameRules.playerMoveSpeed * dt);
        float measured = Mathf.Clamp01(Vector2.Distance(previous, transform.position) / full);
        previous = transform.position;
        pace = Mathf.Lerp(pace, measured, 1 - Mathf.Exp(-dt / .1f));
    }
}
