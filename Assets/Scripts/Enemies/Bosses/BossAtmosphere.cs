using UnityEngine;

// During a boss fight the arena's background darkens a step with every phase, and
// abstract shapes in the district's colour drift across it. The veil sits behind
// every ship and bullet, so only the scenery dims; it fades away once the boss dies.
public sealed class BossAtmosphere : MonoBehaviour
{
    static BossAtmosphere current;
    static Sprite square;
    SpriteRenderer veil;
    ParticleSystem motes;
    float darkness, nextScan, emit;
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
        veil = new GameObject("Phase veil").AddComponent<SpriteRenderer>();
        veil.transform.SetParent(transform, false);
        veil.sprite = square;
        veil.sortingLayerName = "Background";
        veil.sortingOrder = 32000;
        veil.color = Color.clear;
        motes = PSXImpactEffects.Create(transform, "Phase motes", 400);
        var renderer = motes.GetComponent<ParticleSystemRenderer>();
        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = 32001;
        motes.Play();
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
        if (!bossAlive && darkness <= 0)
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
        if (!bossAlive || GameRules.playerDisabled)
            return;
        emit += Time.deltaTime * (4 + 8 * phase);
        var accent = LevelProjectilePalette.Accent(LevelRun.Current);
        while (emit >= 1)
        {
            emit--;
            var p = new ParticleSystem.EmitParams();
            p.position = new Vector3(centre.x + Random.Range(-width, width) * .5f, centre.y + Random.Range(-height, height) * .5f, 11);
            p.velocity = new Vector3(Random.Range(-.3f, .3f), Random.Range(-.2f, .5f), 0);
            float size = Random.Range(.08f, .32f);
            p.startSize3D = new Vector3(size, size * Random.Range(.4f, 1.6f), 1);
            p.rotation = Random.Range(0, 360f);
            p.angularVelocity = Random.Range(-90f, 90f);
            p.startLifetime = Random.Range(2.5f, 4.5f);
            var tint = Color.Lerp(accent, Color.white, Random.Range(0, .45f));
            tint.a = Random.Range(.18f, .4f);
            p.startColor = tint;
            motes.Emit(p, 1);
        }
    }

    void OnDestroy()
    {
        if (current == this)
            current = null;
    }
}
