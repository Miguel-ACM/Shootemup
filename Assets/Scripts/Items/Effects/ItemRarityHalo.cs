using UnityEngine;
// Colour, ring count and orbiting markers all communicate rarity before the preview opens.
public sealed class ItemRarityHalo : MonoBehaviour
{
    AItem item;
    LineRenderer outer, inner;
    SpriteRenderer[] stars;
    float phase;
    void Start()
    {
        item = GetComponent<AItem>();
        phase = Random.value * 6;
        var color = AItem.GetRarityColor(item.rarity);
        outer = CombatVisuals.Line(transform, color, .055f);
        inner = CombatVisuals.Line(transform, color * .8f, .025f);
        int tier = item.rarity == "legendary" || item.rarity == PactPool.Rarity ? 4 : item.rarity == "epic" ? 3 : item.rarity == "rare" ? 2 : 1;
        stars = new SpriteRenderer[tier];
        for (int i = 0; i < tier; i++)
        {
            var go = new GameObject("Rarity spark");
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<SpriteRenderer>();
            s.sprite = CombatVisuals.Disc;
            s.color = color;
            s.sortingLayerName = "Bullet";
            s.sortingOrder = 10;
            stars[i] = s;
        }
        inner.enabled = tier > 1;
    }

    void Update()
    {
        if (item == null)
            return;
        bool visible = !item.Collected && !item.IsDismissing;
        outer.enabled = visible;
        inner.enabled = visible && stars.Length > 1;
        float pulse = 1 + Mathf.Sin(Time.time * 3 + phase) * .06f;
        var p = transform.position + Vector3.back * .7f;
        CombatVisuals.Ring(outer, p, .49f * pulse);
        CombatVisuals.Ring(inner, p, .61f * pulse);
        for (int i = 0; i < stars.Length; i++)
        {
            float a = Time.time * (.8f + stars.Length * .18f) + phase + i * Mathf.PI * 2 / stars.Length;
            stars[i].enabled = visible;
            stars[i].transform.position = p + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * .62f;
            stars[i].transform.localScale = new Vector3(.07f, .13f, 1);
            stars[i].transform.rotation = Quaternion.Euler(0, 0, -a * Mathf.Rad2Deg);
        }
    }
}
