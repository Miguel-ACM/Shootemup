using UnityEngine;

// Ink Pot: a kill leaves a dark puddle that slows the hostile rounds crossing it.
// A fixed, scene-owned pool; the slowdown query reads flat arrays and never allocates.
public sealed class InkPuddle : MonoBehaviour
{
    const int Capacity = 12;
    const float SlowRate = .4f, FadeIn = .15f, FadeOut = .7f;
    static InkPuddle[] pool;
    static readonly Vector2[] centres = new Vector2[Capacity];
    static readonly float[] reach = new float[Capacity]; // squared radius; zero while idle
    static int cursor, live;
    static Sprite splat;
    SpriteRenderer sprite;
    int slot;
    float age, duration, radius;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState()
    {
        pool = null;
        cursor = live = 0;
        System.Array.Clear(reach, 0, Capacity);
        splat = null;
    }

    public static float SlowAt(Vector3 position)
    {
        if (live == 0)
            return 1;
        Vector2 p = position;
        for (int i = 0; i < Capacity; i++)
            if (reach[i] > 0 && (p - centres[i]).sqrMagnitude < reach[i])
                return SlowRate;
        return 1;
    }

    public static void Spill(Vector3 position, int copies)
    {
        Warm();
        var puddle = pool[cursor++ % Capacity];
        puddle.gameObject.SetActive(false);
        puddle.radius = .85f + .15f * (copies - 1);
        puddle.duration = 4 + (copies - 1);
        puddle.age = 0;
        puddle.transform.SetPositionAndRotation(new Vector3(position.x, position.y, 0), Quaternion.Euler(0, 0, Random.value * 360));
        puddle.gameObject.SetActive(true);
        centres[puddle.slot] = position;
        reach[puddle.slot] = puddle.radius * puddle.radius;
        live++;
        puddle.Draw();
        ItemSound.Cue("itemGum", "popcorn", .05f, .7f, .15f);
    }

    static void Warm()
    {
        if (pool != null && pool[0] != null)
            return;
        cursor = live = 0;
        System.Array.Clear(reach, 0, Capacity);
        var root = new GameObject("Ink puddles").transform;
        var block = new MaterialPropertyBlock();
        block.SetColor("_SpriteTint", Color.white);
        block.SetFloat("_Neutralize", 0);
        block.SetFloat("_Gloss", 0);
        block.SetFloat("_Emission", 0);
        pool = new InkPuddle[Capacity];
        for (int i = 0; i < Capacity; i++)
        {
            var go = new GameObject("Ink puddle");
            go.SetActive(false);
            go.transform.SetParent(root, false);
            var puddle = go.AddComponent<InkPuddle>();
            puddle.slot = i;
            puddle.sprite = go.AddComponent<SpriteRenderer>();
            puddle.sprite.sprite = Splat;
            puddle.sprite.sharedMaterial = CombatVisuals.ProjectileMaterial;
            puddle.sprite.sortingLayerName = "Bullet";
            puddle.sprite.sortingOrder = 2;
            puddle.sprite.SetPropertyBlock(block);
            pool[i] = puddle;
        }
    }

    // A dark, irregular splat with a few droplets. Every pixel stays below the shader's
    // enamel threshold, so the puddle reads as matte ink, never as a glowing round.
    static Sprite Splat
    {
        get
        {
            if (splat != null)
                return splat;
            const int Size = 64;
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "Ink puddle" };
            var pixels = new Color[Size * Size];
            var body = new Color(.07f, .06f, .13f, 1);
            var rim = new Color(.15f, .13f, .24f, 1);
            Vector2[] drops = { new Vector2(52, 40), new Vector2(12, 47), new Vector2(46, 10), new Vector2(8, 18), new Vector2(57, 25) };
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    var d = new Vector2(x - 31.5f, y - 31.5f);
                    float angle = Mathf.Atan2(d.y, d.x);
                    float edge = 21 + 2.6f * Mathf.Sin(angle * 3 + 1) + 1.8f * Mathf.Sin(angle * 5 + 2.2f) + 1.1f * Mathf.Sin(angle * 9);
                    float r = d.magnitude;
                    Color c = r < edge - 2 ? body : r < edge ? rim : Color.clear;
                    foreach (var drop in drops)
                        if (Vector2.Distance(new Vector2(x, y), drop) < 3.2f)
                            c = body;
                    pixels[y * Size + x] = c;
                }
            texture.SetPixels(pixels);
            texture.Apply();
            // One world unit of scale spans the splat's main body diameter.
            return splat = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(.5f, .5f), 42);
        }
    }

    void Update()
    {
        if (Time.deltaTime <= 0 || WorldTimeStop.Frozen)
            return;
        age += Time.deltaTime;
        if (age >= duration || GameRules.playerDisabled)
        {
            gameObject.SetActive(false);
            return;
        }
        Draw();
    }

    void Draw()
    {
        float grow = Mathf.Clamp01(age / FadeIn);
        float fade = Mathf.Clamp01((duration - age) / FadeOut);
        transform.localScale = Vector3.one * radius * 2 * Mathf.Lerp(.6f, 1, grow) * Mathf.Lerp(.85f, 1, fade);
        sprite.color = new Color(1, 1, 1, .68f * grow * fade);
    }

    void OnDisable()
    {
        if (reach[slot] <= 0)
            return;
        reach[slot] = 0;
        live--;
    }
}
