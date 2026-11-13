using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class Overdrive : AActiveItem
{
    void Awake()
    {
        SetData("Overdrive", "Active", "Double damage for 6 seconds. 60 energy.", ItemType.active);
        cost = 60;
        cd = 4;
    }

    public override float OnPress(float mana)
    {
        float valid = CheckValidPress(mana);
        if (valid < 0)
            return valid;
        var boost = s.GetComponent<OverdriveBoost>();
        if (boost == null)
            boost = s.gameObject.AddComponent<OverdriveBoost>();
        boost.until = Time.time + 6;
        ItemSound.Play("overdrive", .22f);
        ItemSpark.Emit(s.transform.position, new Color(1, .55f, .2f), 8, .15f, true);
        lastCD = 0;
        return mana - cost;
    }
}
public sealed class OverdriveBoost : MonoBehaviour
{
    public float until;
    SpriteRenderer left, right;
    PSXShipEffects engines;
    float animationTime;
    void Awake()
    {
        GameRules.playerShootDamage *= 2;
    }

    void Start()
    {
        engines = GetComponent<PSXShipEffects>();
        left = ItemSpark.Sprite(transform, "Overdrive port", "needle", new Color(1, .55f, .2f), .28f);
        right = ItemSpark.Sprite(transform, "Overdrive starboard", "needle", new Color(1, .55f, .2f), .28f);
        left.transform.position = transform.position + new Vector3(-.3f, -.25f, -2);
        right.transform.position = transform.position + new Vector3(.3f, -.25f, -2);
        left.transform.localScale = right.transform.localScale = new Vector3(.18f / transform.lossyScale.x, .18f / transform.lossyScale.y, 1);
        left.sortingLayerName = right.sortingLayerName = "Player";
        left.sortingOrder = right.sortingOrder = 3;
    }

    void Update()
    {
        // Keep the six-second boost through rewards, transitions and pauses.
        if (!GameClock.ActivePlay)
        {
            until += Time.deltaTime;
            return;
        }
        animationTime += GameClock.ActivePlayDelta;
        if (Time.time > until)
        {
            ItemSound.Play("powerdown", .17f);
            ItemSpark.Emit(transform.position, new Color(1, .55f, .2f), 4, .1f);
            Destroy(this);
            return;
        }
        float fade = Mathf.Clamp01(until - Time.time);
        float pulse = .5f + .5f * Mathf.Sin(animationTime * 18);
        if (engines != null) engines.OverdriveIntensity = fade * (.65f + .35f * pulse);
        float a = fade * (.65f + .3f * pulse);
        if (left != null)
        {
            left.color = right.color = new Color(1, 1, 1, a);
            // Twin amber pulses climb the hull; the cockpit and incoming rounds stay clear.
            float sweep = Mathf.Repeat(animationTime * 2.4f, 1);
            float spread = .27f + .07f * Mathf.Sin(sweep * Mathf.PI);
            left.transform.position = transform.position + new Vector3(-spread, Mathf.Lerp(-.38f, .32f, sweep), -2);
            right.transform.position = transform.position + new Vector3(spread, Mathf.Lerp(-.38f, .32f, sweep), -2);
            Vector3 size = new Vector3((.15f + .05f * pulse) / transform.lossyScale.x, (.24f + .08f * pulse) / transform.lossyScale.y, 1);
            left.transform.localScale = right.transform.localScale = size;
        }
    }

    void OnDestroy()
    {
        if (engines != null) engines.OverdriveIntensity = 0;
        GameRules.playerShootDamage *= .5f;
        if (left != null)
            Destroy(left.gameObject);
        if (right != null)
            Destroy(right.gameObject);
    }
}
