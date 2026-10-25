using UnityEngine;

// Reuse the approved neutral pixels: all gum colour comes from code, including the pop.
public sealed class GumSpritePop : MonoBehaviour
{
    const float Duration = .24f;
    static int live;
    readonly SpriteRenderer[] sprites = new SpriteRenderer[5];
    float age;
    public static void Spawn(Vector3 position)
    {
        if (live >= 64)
            return;
        var go = new GameObject("Gum sprite pop");
        go.transform.position = position;
        var pop = go.AddComponent<GumSpritePop>();
        for (int i = 0; i < pop.sprites.Length; i++)
        {
            var part = new GameObject(i == 0 ? "Pop flash" : "Gum fleck");
            part.transform.SetParent(go.transform, false);
            var sprite = part.AddComponent<SpriteRenderer>();
            sprite.sprite = ProjectileSprites.Load("standard");
            ProjectileSpriteTint.Apply(part, new Color(.95f, .35f, .65f));
            sprite.sortingOrder = 25;
            part.transform.localScale = Vector3.one * (i == 0 ? .39f : .075f);
            pop.sprites[i] = sprite;
        }
    }

    void OnEnable()
    {
        live++;
    }

    void OnDisable()
    {
        live--;
    }

    void Update()
    {
        if (WorldTimeStop.Frozen || Time.deltaTime <= 0)
            return;
        age += Time.deltaTime;
        float t = Mathf.Clamp01(age / Duration);
        for (int i = 0; i < sprites.Length; i++)
        {
            var sprite = sprites[i];
            sprite.color = new Color(1, 1, 1, i == 0 ? Mathf.Clamp01(1 - t * 3) : 1 - t);
            if (i == 0)
                sprite.transform.localScale = Vector3.one * Mathf.Lerp(.39f, .55f, t);
            else
            {
                float angle = (i - 1) * Mathf.PI * .5f + Mathf.PI * .25f;
                sprite.transform.localPosition = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * Mathf.Lerp(.08f, .35f, t);
                sprite.transform.localScale = Vector3.one * Mathf.Lerp(.075f, .025f, t);
            }
        }
        if (age >= Duration)
            Destroy(gameObject);
    }
}
