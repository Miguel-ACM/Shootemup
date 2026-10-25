using UnityEngine;

// Approved neutral sprite silhouettes, code tint, no replacement projectile artwork.
public sealed class ItemSpark : MonoBehaviour
{
    static int live;
    SpriteRenderer sprite;
    Vector3 velocity;
    float age, duration, size;
    bool playerClock;
    public static SpriteRenderer Sprite(Transform parent, string name, string art, Color color, float size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ProjectileSprites.Load(art);
        sr.sharedMaterial = CombatVisuals.ProjectileMaterial;
        sr.sortingLayerName = "Bullet";
        sr.sortingOrder = 23;
        var block = new MaterialPropertyBlock();
        block.SetColor("_SpriteTint", color);
        block.SetFloat("_Neutralize", 0);
        sr.SetPropertyBlock(block);
        go.transform.localScale = Vector3.one * size;
        return sr;
    }

    public static void Emit(Vector3 p, Color color, int count = 5, float size = .14f, bool playerClock = false, string art = "standard")
    {
        for (int i = 0; i < count && live < 100; i++)
        {
            var sr = Sprite(null, "Item spark", art, color, size);
            sr.transform.position = new Vector3(p.x, p.y, -2);
            var fx = sr.gameObject.AddComponent<ItemSpark>();
            fx.sprite = sr;
            fx.duration = .32f;
            fx.size = size;
            fx.playerClock = playerClock;
            fx.velocity = Quaternion.Euler(0, 0, i * 360f / count + 25) * Vector3.up * 1.5f;
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
        float dt = playerClock ? WorldTimeStop.PlayerDelta : Time.deltaTime;
        if (dt <= 0)
            return;
        age += dt;
        transform.position += velocity * dt;
        float t = age / duration;
        sprite.color = new Color(1, 1, 1, 1 - t);
        transform.localScale = Vector3.one * size * (1 - t * .6f);
        if (age >= duration)
            Destroy(gameObject);
    }
}
