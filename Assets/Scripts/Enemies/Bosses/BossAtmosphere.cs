using UnityEngine;

// During a boss fight the arena's background darkens a step with every phase and a
// mist in the district's colour thickens over the scenery. Veil and mist sit behind
// every ship and bullet, so only the scenery dims; both fade away once the boss dies.
public sealed class BossAtmosphere : MonoBehaviour
{
    const int Banks = 14;
    static BossAtmosphere current;
    static Sprite square, puff;
    SpriteRenderer veil;
    readonly SpriteRenderer[] mist = new SpriteRenderer[Banks];
    readonly Vector2[] anchors = new Vector2[Banks];
    readonly float[] drift = new float[Banks], breath = new float[Banks];
    float darkness, density, nextScan;
    int phase;
    bool bossAlive;

    public static void Begin()
    {
        if (current != null)
            return;
        current = new GameObject("Boss atmosphere").AddComponent<BossAtmosphere>();
    }

    void Awake()
    {
        if (square == null)
            square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(.5f, .5f), 4);
        if (puff == null)
            puff = MistPuff();
        veil = new GameObject("Phase veil").AddComponent<SpriteRenderer>();
        veil.transform.SetParent(transform, false);
        veil.sprite = square;
        veil.sortingLayerName = "Background";
        veil.sortingOrder = 32000;
        veil.color = Color.clear;
        for (int i = 0; i < Banks; i++)
        {
            var bank = new GameObject("Phase mist").AddComponent<SpriteRenderer>();
            bank.transform.SetParent(transform, false);
            bank.sprite = puff;
            bank.sortingLayerName = "Background";
            bank.sortingOrder = 32001;
            bank.color = Color.clear;
            bank.flipX = i % 2 == 1;
            bank.flipY = i % 3 == 1;
            mist[i] = bank;
            // Banks spread over a grid with jitter so the haze covers the view evenly.
            anchors[i] = new Vector2((i % 4 + Random.Range(.1f, .9f)) / 4, (i / 4 + Random.Range(.1f, .9f)) / 4);
            drift[i] = Random.Range(.05f, .14f) * (i % 2 == 0 ? 1 : -1);
            breath[i] = Random.Range(0, Mathf.PI * 2);
        }
    }

    // A soft lumpy cloud: a few overlapping gaussian blobs, generated once.
    static Sprite MistPuff()
    {
        const int Size = 64;
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[Size * Size];
        var lumps = new[] { new Vector3(.5f, .5f, .30f), new Vector3(.32f, .56f, .20f), new Vector3(.68f, .44f, .22f), new Vector3(.46f, .34f, .18f), new Vector3(.58f, .66f, .17f) };
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float u = (x + .5f) / Size, v = (y + .5f) / Size, a = 0;
                foreach (var lump in lumps)
                {
                    float dx = u - lump.x, dy = v - lump.y;
                    a = Mathf.Max(a, Mathf.Exp(-(dx * dx + dy * dy) / (2 * lump.z * lump.z * .25f)));
                }
                float edge = Mathf.Clamp01((.5f - Mathf.Max(Mathf.Abs(u - .5f), Mathf.Abs(v - .5f))) * 8);
                pixels[y * Size + x] = new Color32(255, 255, 255, (byte)(a * edge * 255));
            }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(.5f, .5f), Size);
    }

    void Update()
    {
        var view = UnityEngine.Camera.main;
        if (view == null)
            return;
        if (Time.time > nextScan)
        {
            nextScan = Time.time + .25f;
            bossAlive = false;
            phase = 0;
            foreach (var boss in FindObjectsByType<DistrictBoss>())
                if (boss.UsesHealthBar && boss.health > 0)
                {
                    bossAlive = true;
                    phase = Mathf.Max(phase, boss.CurrentPhase);
                }
        }
        float target = bossAlive ? .12f + .14f * (phase - 1) : 0;
        darkness = Mathf.MoveTowards(darkness, target, Time.deltaTime * .25f);
        // The mist thickens with every phase and clears once the boss is gone.
        float thickness = bossAlive ? .12f + .11f * (phase - 1) : 0;
        density = Mathf.MoveTowards(density, thickness, Time.deltaTime * (bossAlive ? .2f : .3f));
        if (!bossAlive && darkness <= 0 && density <= 0)
        {
            Destroy(gameObject);
            return;
        }
        // Behind ships and bullets, in front of the scenery, covering the whole view.
        var centre = view.transform.position;
        float height = view.orthographicSize * 2 + 2, width = height * view.aspect + 2;
        veil.transform.position = new Vector3(centre.x, centre.y, 12);
        veil.transform.localScale = new Vector3(width, height, 1);
        veil.color = new Color(0, 0, 0, darkness);
        var accent = Color.Lerp(LevelProjectilePalette.Accent(LevelRun.Current), Color.white, .25f);
        float time = Time.time;
        for (int i = 0; i < Banks; i++)
        {
            float u = Mathf.Repeat(anchors[i].x + time * drift[i] / width * 4, 1);
            float v = anchors[i].y + Mathf.Sin(time * .13f + breath[i]) * .03f;
            float swell = .8f + .2f * Mathf.Sin(time * .21f + breath[i] * 1.7f);
            var bank = mist[i];
            bank.transform.position = new Vector3(centre.x + (u - .5f) * (width + 6), centre.y + (v - .5f) * height, 11);
            bank.transform.localScale = new Vector3(width * .42f, height * .26f, 1) * swell;
            accent.a = density * swell;
            bank.color = accent;
        }
    }

    void OnDestroy()
    {
        if (current == this)
            current = null;
    }
}
