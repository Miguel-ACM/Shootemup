using System.Collections.Generic;
using UnityEngine;

// A charged impact leaves a bounded, stationary damage field, not another volley.
public sealed class ChargeBlast : MonoBehaviour
{
    const int Limit = 16;
    static readonly List<ChargeBlast> live = new List<ChargeBlast>();
    readonly List<AEnemy> targets = new List<AEnemy>();
    static readonly int BlastState = Shader.PropertyToID("_BlastState");
    static Material plasma;
    static Mesh quad;
    MeshRenderer field;
    MaterialPropertyBlock properties;
    bool fullCharge, companion;
    float age, duration, radius, totalDamage, nextTick;

    void Awake()
    {
        if (quad == null)
            quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        if (plasma == null)
            plasma = new Material(Resources.Load<Shader>("ChargeDetonation"));
        gameObject.AddComponent<MeshFilter>().sharedMesh = quad;
        field = gameObject.AddComponent<MeshRenderer>();
        field.sharedMaterial = plasma;
        field.sortingLayerName = "Bullet";
        field.sortingOrder = 13;
        field.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        field.receiveShadows = false;
        properties = new MaterialPropertyBlock();
    }

    public static void Spawn(Vector3 position, float damage, int level, bool companion = false)
    {
        live.RemoveAll(blast => blast == null);
        ChargeBlast blast;
        if (live.Count >= Limit)
        {
            blast = live[0];
            live.RemoveAt(0);
        }
        else
            blast = new GameObject("Lingering charge detonation").AddComponent<ChargeBlast>();
        blast.transform.position = new Vector3(position.x, position.y, -1);
        blast.age = blast.nextTick = 0;
        blast.companion = companion;
        blast.fullCharge = level >= 3;
        blast.duration = blast.fullCharge ? 1.4f : .9f;
        blast.radius = (blast.fullCharge ? 1.35f : .85f) * Mathf.Sqrt(GameRules.playerShootSize);
        blast.totalDamage = damage;
        live.Add(blast);
        blast.DrawField();
        ItemSound.Cue("itemChargeBlast", "cannon", .14f, .8f, .16f);
    }

    void Update()
    {
        if (Menu.GameplayPaused || WorldTimeStop.Frozen || Time.deltaTime <= 0)
            return;
        if (GameRules.playerDisabled)
        {
            Destroy(gameObject);
            return;
        }
        age += Time.deltaTime;
        DrawField();
        // Fixed slices preserve total damage across frame rates, including the last slice.
        while (nextTick < duration && age >= nextTick)
        {
            float slice = Mathf.Min(.1f, duration - nextTick);
            float tickRadius = RadiusAt(nextTick);
            targets.Clear();
            targets.AddRange(AEnemy.Live);
            foreach (var enemy in targets)
            {
                if (enemy == null || !enemy.CanBeTargeted)
                    continue;
                if (!enemy.ClosestDamagePoint(transform.position, out var point) || (point - (Vector2)transform.position).sqrMagnitude > tickRadius * tickRadius)
                    continue;
                float amount = totalDamage * slice / duration;
                CombatHit.Apply(enemy, amount, point, Vector2.up, generation: 1, companion: companion);
            }
            nextTick += slice;
        }
        if (age >= duration)
            Destroy(gameObject);
    }

    void DrawField()
    {
        transform.localScale = Vector3.one * (2 * RadiusAt(age));
        properties.SetVector(BlastState, new Vector4(age, duration, fullCharge ? 1 : 0, 0));
        field.SetPropertyBlock(properties);
    }

    float RadiusAt(float t) => radius * Mathf.Lerp(.35f, 1, Mathf.Clamp01(t / .16f));
    void OnDestroy()
    {
        live.Remove(this);
    }
}
