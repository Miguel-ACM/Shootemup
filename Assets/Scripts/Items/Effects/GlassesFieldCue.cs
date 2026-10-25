using UnityEngine;
using System.Collections.Generic;

public sealed class GlassesFieldCue : MonoBehaviour
{
    readonly HashSet<CombatRound> inside = new HashSet<CombatRound>();
    LineRenderer boundary;
    float next;
    void Start()
    {
        boundary = CombatVisuals.Line(transform, new Color(.5f, .8f, .75f, .12f), .025f);
        boundary.positionCount = 33;
        ItemSpark.Emit(transform.position, new Color(.5f, .8f, .75f), 6, .12f);
    }

    void Update()
    {
        var upgrades = GetComponent<CombatUpgrades>();
        bool local = upgrades.Count(UpgradeKind.BadGlasses) > 0;
        boundary.enabled = local && !GameRules.playerDisabled;
        if (boundary.enabled)
            for (int i = 0; i < 33; i++)
            {
                float a = i * Mathf.PI / 16;
                var p = transform.position + new Vector3(Mathf.Cos(a) * 3, Mathf.Sin(a) * 3, -2);
                boundary.SetPosition(i, p);
            }
        if (Time.deltaTime <= 0 || GameRules.playerDisabled)
            return;
        inside.RemoveWhere(b => b == null || !b.isActiveAndEnabled || (local && Vector2.Distance(b.transform.position, transform.position) > 3));
        foreach (var bullet in CombatRound.Live)
        {
            if (bullet is PulseLaser || (local && Vector2.Distance(bullet.transform.position, transform.position) > 3))
                continue;
            if (!inside.Add(bullet) || Time.time < next)
                continue;
            next = Time.time + .15f;
            ItemSpark.Emit(bullet.transform.position, new Color(.5f, .8f, .75f), 2, .085f);
        }
    }

    void OnDestroy()
    {
        if (boundary != null)
            Destroy(boundary.gameObject);
    }
}
