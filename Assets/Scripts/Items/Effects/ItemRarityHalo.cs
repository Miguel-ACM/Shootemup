using UnityEngine;
// A soft pulse in the rarity colour: rings expand from the item and fade, one ring per rarity tier in each beat.
public sealed class ItemRarityHalo : MonoBehaviour
{
    const float Beat = 1.8f, Stagger = .16f, Grow = .7f;
    AItem item;
    LineRenderer[] rings;
    Color color;
    float phase;
    void Start()
    {
        item = GetComponent<AItem>();
        phase = Random.value * Beat;
        color = AItem.GetRarityColor(item.rarity);
        int tier = item.rarity == "legendary" || item.rarity == PactPool.Rarity ? 4 : item.rarity == "epic" ? 3 : item.rarity == "rare" ? 2 : 1;
        rings = new LineRenderer[tier];
        for (int i = 0; i < tier; i++)
            rings[i] = CombatVisuals.Line(transform, color, .02f);
    }

    void Update()
    {
        if (item == null)
            return;
        bool visible = !item.Collected && !item.IsDismissing;
        var p = transform.position + Vector3.back * .7f;
        for (int i = 0; i < rings.Length; i++)
        {
            float t = (Time.time + phase - i * Stagger) % Beat / Grow;
            bool live = visible && t >= 0 && t < 1;
            rings[i].enabled = live;
            if (!live)
                continue;
            var c = color;
            c.a = .4f * (1 - t) * (1 - t);
            rings[i].startColor = rings[i].endColor = c;
            CombatVisuals.Ring(rings[i], p, Mathf.Lerp(.3f, .8f, 1 - (1 - t) * (1 - t)));
        }
    }
}
