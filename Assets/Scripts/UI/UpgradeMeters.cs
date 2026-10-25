using UnityEngine;
public sealed class UpgradeMeters : MonoBehaviour
{
    LineRenderer charge, salvage, reserve;
    void Start()
    {
        charge = CombatVisuals.Line(transform, new Color(.4f, 1, .9f), .065f);
        salvage = CombatVisuals.Line(transform, new Color(1, .8f, .3f), .05f);
        reserve = CombatVisuals.Line(transform, new Color(.6f, .65f, 1), .05f);
    }

    void Update()
    {
        var u = GetComponent<CombatUpgrades>();
        if (u == null)
            return;
        Meter(charge, u.Count(UpgradeKind.Capacitor) > 0, u.Charge, -.58f);
        Meter(salvage, u.Count(UpgradeKind.Salvage) > 0, u.SalvageProgress, -.72f);
        Meter(reserve, u.Count(UpgradeKind.Reserve) > 0, u.Reserve / 12, -.86f);
    }

    void Meter(LineRenderer line, bool visible, float fill, float y)
    {
        line.enabled = visible;
        var p = transform.position + new Vector3(-.35f, y, -2);
        line.SetPosition(0, p);
        line.SetPosition(1, p + Vector3.right * Mathf.Max(.02f, .7f * fill));
    }
}
