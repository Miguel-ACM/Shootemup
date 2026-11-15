using System.Collections.Generic;
using UnityEngine;

// Midas Touch: a kill sometimes turns the hostile rounds around it to gold. Up to eight of
// them become real coins where they were; the rest just vanish in a golden flash. The chance
// grows with luck and with extra copies.
[UnityEngine.Scripting.Preserve]
public sealed class MidasTouch : AItem
{
    public const float Chance = .3f, ExtraChance = .15f, Radius = 1.8f;
    const int MaxCoins = 8;
    static readonly Color Gold = new Color(1, .8f, .26f);
    static readonly List<CombatRound> touched = new List<CombatRound>();

    void Awake()
    {
        SetData("Midas Touch", "Everything turns to gold", "Kills can turn nearby enemy fire into coins.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Midas);
        base.OnPickup();
    }

    public static void Touch(AEnemy dead, int copies)
    {
        if (copies <= 0 || dead == null)
            return;
        float chance = Mathf.Clamp01(Chance + ExtraChance * (copies - 1) + GameRules.luck);
        if (Random.value >= chance)
            return;
        Vector2 at = dead.transform.position;
        touched.Clear();
        foreach (var round in CombatRound.Live)
            if (BulletReflection.CanReflect(round) && ((Vector2)round.transform.position - at).sqrMagnitude <= Radius * Radius)
                touched.Add(round);
        if (touched.Count == 0)
            return;
        var prefab = Resources.Load<GameObject>("Pickables/money");
        for (int i = 0; i < touched.Count; i++)
        {
            var p = touched[i].transform.position;
            ItemSpark.Emit(p, Gold, 2, .07f);
            if (i < MaxCoins && prefab != null)
            {
                var spot = new Vector3(Mathf.Clamp(p.x, Camera.LeftLimit + .25f, Camera.RightLimit - .25f),
                    Mathf.Clamp(p.y, Camera.DownLimit + .25f, Camera.UpLimit - .25f), 1);
                var coin = Object.Instantiate(prefab, spot, Quaternion.identity).GetComponent<Money>();
                coin.Launch(Random.insideUnitCircle * .8f, .25f);
            }
            touched[i].Vanish();
        }
        RunInventory.Ping(UpgradeKind.Midas);
        PSXFireballs.Shockwave(at, Radius * 1.3f, .45f, Gold, 2, .8f);
        ItemSound.Cue("itemSalvage", volume: .12f, pitch: 1.3f, cooldown: .15f);
    }
}
