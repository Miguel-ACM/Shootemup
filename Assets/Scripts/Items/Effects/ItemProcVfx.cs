using UnityEngine;

public enum ItemProcShape { Fork, Rebound, Counter, Convert, Charge, Ignite, Toxic, Detonate, Lock, Return }

// Prepared once behind the curtain. Short accents explain a trigger, beneath live rounds.
public sealed class ItemProcVfx : MonoBehaviour
{
    const int Capacity = 20, Points = 17;
    static ItemProcVfx[] pool;
    static int cursor;
    static readonly float[] gates = new float[10];
    readonly LineRenderer[] strokes = new LineRenderer[2];
    ItemProcShape shape;
    Color ink;
    float age, span, duration;
    Vector2 direction;
    bool playing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState()
    {
        pool = null;
        cursor = 0;
        System.Array.Clear(gates, 0, gates.Length);
    }

    public static void Warm()
    {
        if (pool != null && pool[0] != null) return;
        var root = new GameObject("Prepared item trigger accents");
        pool = new ItemProcVfx[Capacity];
        for (int i = 0; i < Capacity; i++)
        {
            var go = new GameObject("Item trigger accent");
            go.transform.SetParent(root.transform, false);
            var effect = go.AddComponent<ItemProcVfx>();
            for (int stroke = 0; stroke < 2; stroke++)
            {
                var line = CombatVisuals.Line(go.transform, Color.white, .035f);
                line.positionCount = Points;
                line.sortingOrder = 13;
                line.enabled = false;
                effect.strokes[stroke] = line;
            }
            pool[i] = effect;
        }
    }

    public static void Emit(ItemProcShape shape, Vector3 position, Color color, float size = .45f, Vector2 direction = default)
    {
        if (GameRules.playerDisabled || Menu.GameplayPaused || Time.time < gates[(int)shape]) return;
        gates[(int)shape] = Time.time + .075f;
        Warm();
        var effect = pool[cursor++ % Capacity];
        effect.transform.position = new Vector3(position.x, position.y, -1.1f);
        effect.shape = shape;
        effect.ink = color;
        effect.span = Mathf.Clamp(size, .15f, 1.3f);
        effect.direction = direction.sqrMagnitude > .01f ? direction.normalized : Vector2.up;
        effect.age = 0;
        effect.duration = shape == ItemProcShape.Lock ? .4f : .28f;
        effect.playing = true;
        effect.Draw();
    }

    void Update()
    {
        if (!playing || Menu.GameplayPaused || WorldTimeStop.Frozen || Time.deltaTime <= 0) return;
        age += Time.deltaTime;
        if (age >= duration || GameRules.playerDisabled)
        {
            playing = false;
            foreach (var stroke in strokes) stroke.enabled = false;
            return;
        }
        Draw();
    }

    void Draw()
    {
        float t = Mathf.Clamp01(age / duration);
        float fade = Mathf.Sin(Mathf.Clamp01((1 - t) * 1.6f) * Mathf.PI * .5f);
        Vector2 right = new Vector2(direction.y, -direction.x);
        for (int s = 0; s < 2; s++)
        {
            int sign = s == 0 ? -1 : 1;
            var line = strokes[s];
            line.enabled = true;
            Color tint = ink;
            tint.a = .7f * fade;
            line.startColor = line.endColor = tint;
            line.startWidth = line.endWidth = .035f * Mathf.Lerp(1, .35f, t);
            for (int i = 0; i < Points; i++)
            {
                float p = i / (float)(Points - 1);
                Vector2 local;
                switch (shape)
                {
                    case ItemProcShape.Fork:
                        local = new Vector2(sign * Mathf.Max(0, p - .35f), p - .25f) * Mathf.Lerp(.4f, 1.2f, t);
                        break;
                    case ItemProcShape.Rebound:
                        local = p < .5f ? Vector2.Lerp(new Vector2(-.75f, -.4f), Vector2.zero, p * 2) : Vector2.Lerp(Vector2.zero, new Vector2(.65f, .5f), (p - .5f) * 2);
                        local.y += sign * .07f;
                        local *= Mathf.Lerp(.65f, 1.2f, t);
                        break;
                    case ItemProcShape.Counter:
                        float shield = Mathf.Lerp(-155, -25, p) * Mathf.Deg2Rad;
                        local = s == 0 ? new Vector2(Mathf.Cos(shield), Mathf.Sin(shield)) : new Vector2((p < .5f ? p : 1 - p) * .5f, p * 1.2f);
                        break;
                    case ItemProcShape.Convert:
                        local = new Vector2(Mathf.Cos(p * Mathf.PI * 2), Mathf.Sin(p * Mathf.PI * 2)) * Mathf.Lerp(s == 0 ? .7f : .4f, .1f, t);
                        local.y += t * .9f;
                        break;
                    case ItemProcShape.Charge:
                        float charge = p * Mathf.PI * 1.6f + s * Mathf.PI + t * 3;
                        local = new Vector2(Mathf.Cos(charge), Mathf.Sin(charge)) * Mathf.Lerp(.85f, .35f, t);
                        break;
                    case ItemProcShape.Ignite:
                        local = new Vector2(sign * Mathf.Sin(p * Mathf.PI) * (.3f + .08f * Mathf.Sin(t * 8)), p * 1.3f + t * .4f);
                        break;
                    case ItemProcShape.Toxic:
                        float bubble = p * Mathf.PI * 2;
                        local = new Vector2(sign * .3f + Mathf.Cos(bubble) * .18f, Mathf.Sin(bubble) * .18f + t * .8f + s * .25f);
                        break;
                    case ItemProcShape.Detonate:
                        float blast = p * Mathf.PI * 2;
                        float lobes = 1 + .13f * Mathf.Cos(blast * 6 + s * Mathf.PI);
                        local = new Vector2(Mathf.Cos(blast), Mathf.Sin(blast)) * lobes * Mathf.Lerp(.15f, s == 0 ? 1.1f : .7f, t);
                        break;
                    case ItemProcShape.Lock:
                        float bracket = Mathf.Lerp(35, 145, p) * Mathf.Deg2Rad + s * Mathf.PI;
                        local = new Vector2(Mathf.Cos(bracket), Mathf.Sin(bracket)) * Mathf.Lerp(1, .6f, t);
                        break;
                    default:
                        float turn = Mathf.Lerp(-45, 225, p) * Mathf.Deg2Rad;
                        local = new Vector2(Mathf.Cos(turn), Mathf.Sin(turn)) * (.7f + s * .12f) * Mathf.Lerp(.6f, 1, t);
                        break;
                }
                Vector2 world = right * local.x + direction * local.y;
                line.SetPosition(i, transform.position + (Vector3)(world * span));
            }
        }
    }
}
