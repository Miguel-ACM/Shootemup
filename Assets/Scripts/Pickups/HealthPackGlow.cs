using UnityEngine;

// Health only comes from bosses now, so its pickups shine: a breathing warm halo, a ring and a twinkling cross glint.
public sealed class HealthPackGlow : MonoBehaviour
{
    static readonly Color Warm = new Color(1f, .32f, .26f);
    static Sprite halo;
    SpriteRenderer glow;
    SpriteRenderer[] glints;
    LineRenderer ring;
    float phase;

    void Start()
    {
        phase = Random.value * 6;
        glow = Child("Health glow", Halo, new Color(Warm.r, Warm.g, Warm.b, .5f), "Default", -10, new Vector3(0, 0, .6f));
        glints = new[]
        {
            Child("Health glint", CombatVisuals.Disc, new Color(1, .9f, .86f), "Bullet", 11, new Vector3(0, 0, -.8f)),
            Child("Health glint", CombatVisuals.Disc, new Color(1, .9f, .86f), "Bullet", 11, new Vector3(0, 0, -.8f))
        };
        ring = CombatVisuals.Line(transform, Warm, .04f);
    }

    SpriteRenderer Child(string name, Sprite sprite, Color color, string layer, int order, Vector3 offset)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = offset;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingLayerName = layer;
        renderer.sortingOrder = order;
        return renderer;
    }

    void Update()
    {
        float t = Time.time + phase;
        float breath = .5f + .5f * Mathf.Sin(t * 4);
        // Children follow the body but never its tumble.
        glow.transform.rotation = Quaternion.identity;
        glow.transform.localScale = Vector3.one * (1.25f + .25f * breath);
        var c = glow.color;
        c.a = .35f + .3f * breath;
        glow.color = c;
        CombatVisuals.Ring(ring, transform.position + Vector3.back * .7f, .42f + .05f * breath);
        // A sharp twinkle once a second: the cross flares, turns a little and fades.
        float flash = Mathf.Repeat(t, 1.1f) / .35f;
        float size = flash < 1 ? Mathf.Sin(flash * Mathf.PI) : 0;
        for (int i = 0; i < glints.Length; i++)
        {
            glints[i].enabled = size > .01f;
            glints[i].transform.rotation = Quaternion.Euler(0, 0, 20 + i * 90 + flash * 25);
            glints[i].transform.localScale = new Vector3(.06f, .75f, 1) * size;
            glints[i].transform.position = transform.position + new Vector3(.12f, .14f, -.8f);
        }
    }

    static Sprite Halo
    {
        get
        {
            if (halo != null)
                return halo;
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "Health pickup halo" };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Clamp01(Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f)) / 15.5f);
                    pixels[y * size + x] = new Color(1, 1, 1, (1 - d) * (1 - d));
                }
            texture.SetPixels(pixels);
            texture.Apply();
            halo = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
            return halo;
        }
    }
}
