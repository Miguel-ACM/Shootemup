using UnityEngine;
using System.Collections.Generic;

// Approved neutral sprite silhouettes, code tint, no replacement projectile artwork.
public sealed class ItemSpark : MonoBehaviour
{
    static int live;
    static readonly Stack<ItemSpark> pool = new Stack<ItemSpark>();
    static MaterialPropertyBlock tintBlock;
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
            while (pool.Count > 0 && pool.Peek() == null)
                pool.Pop();
            ItemSpark fx;
            if (pool.Count > 0)
                fx = pool.Pop();
            else
            {
                var created = Sprite(null, "Item spark", art, color, size);
                fx = created.gameObject.AddComponent<ItemSpark>();
                fx.sprite = created;
            }
            var sr = fx.sprite;
            sr.sprite = ProjectileSprites.Load(art);
            if (tintBlock == null)
                tintBlock = new MaterialPropertyBlock();
            tintBlock.Clear();
            tintBlock.SetColor("_SpriteTint", color);
            tintBlock.SetFloat("_Neutralize", 0);
            sr.SetPropertyBlock(tintBlock);
            sr.color = Color.white;
            sr.transform.position = new Vector3(p.x, p.y, -2);
            fx.age = 0;
            fx.duration = .32f;
            fx.size = size;
            fx.playerClock = playerClock;
            fx.velocity = Quaternion.Euler(0, 0, i * 360f / count + 25) * Vector3.up * 1.5f;
            if (!fx.gameObject.activeSelf)
                fx.gameObject.SetActive(true);
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
        {
            gameObject.SetActive(false);
            pool.Push(this);
        }
    }
}
