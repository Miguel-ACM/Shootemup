using System;
using System.Collections;
using UnityEngine;

public enum BossDestructionStage
{
    Burning, Freeze, Breakup, Complete
}

// Intact-hull explosions flow continuously into breakup without artificial frame holds.
public sealed class BossDestruction : MonoBehaviour
{
    public const float FinalFreezeSeconds = 0;
    public static BossDestruction Current
    {
        get; private set;
    }

    public static bool HoldingFrame => Current != null && Current.holding;
    public BossDestructionStage Stage
    {
        get; private set;
    }

    public AEnemy Source
    {
        get; private set;
    }

    public PSXPolygonDebris Wreckage
    {
        get; private set;
    }

    public int PreBlasts
    {
        get; private set;
    }

    public float FreezeStarted
    {
        get; private set;
    }

    public float ShatteredAt
    {
        get; private set;
    }

    Bounds bounds;
    Action<Vector3> completed;
    bool holding => false;

    public static void Play(AEnemy source, Action<Vector3> completed)
    {
        var effect = new GameObject("Boss destruction sequence").AddComponent<BossDestruction>();
        effect.Source = source;
        effect.completed = completed;
        Current = effect;
        effect.StartCoroutine(effect.Perform());
    }

    IEnumerator Perform()
    {
        if (WorldTimeStop.Current != null)
            WorldTimeStop.Current.Cancel();
        CombatDecision.Begin();
        var player = CombatVisuals.Player;
        if (player != null)
            foreach (var weapon in player.GetComponentsInChildren<AWeapon>())
                weapon.StopFiring();
        CombatRound.ClearAll();
        foreach (var shot in FindObjectsByType<AEnemyBullet>())
            shot.Vanish();
        foreach (var shot in FindObjectsByType<ABullet>())
            Destroy(shot.gameObject);
        foreach (var enemy in FindObjectsByType<AEnemy>())
            if (enemy != Source && !enemy.transform.IsChildOf(Source.transform))
                Destroy(enemy.gameObject);
        Source.StopAllCoroutines();
        Source.CancelInvoke();
        Source.enabled = false;
        foreach (var mover in Source.GetComponents<AMover>())
            mover.enabled = false;
        var body = Source.GetComponent<Rigidbody2D>();
        if (body != null)
            body.linearVelocity = Vector2.zero;
        foreach (var collider in Source.GetComponentsInChildren<Collider2D>())
            collider.enabled = false;
        var attitude = Source.GetComponent<CombatHullVisual>();
        if (attitude != null)
            attitude.enabled = false;
        var engines = Source.GetComponent<EnemyThrusters>();
        if (engines != null)
            engines.Extinguish();
        var sprite = Source.GetComponent<SpriteRenderer>();
        if (sprite != null)
            sprite.color = Color.white;
        var hulls = Source.GetComponentsInChildren<MeshRenderer>();
        bounds = new Bounds(Source.transform.position, Vector3.zero);
        bool first = true;
        foreach (var hull in hulls)
        {
            if (!hull.enabled)
                continue;
            if (first)
            {
                bounds = hull.bounds;
                first = false;
            }
            else
                bounds.Encapsulate(hull.bounds);
            var ink = new MaterialPropertyBlock();
            hull.GetPropertyBlock(ink);
            var tint = ink.HasColor("_Tint") ? ink.GetColor("_Tint") : Color.white;
            tint.a = 1;
            ink.SetColor("_Tint", tint);
            hull.SetPropertyBlock(ink);
        }
        // Build wreckage while the intact model is still visible; release only on the final accent.
        Wreckage = PSXPolygonDebris.Spawn(Source.gameObject, true, true);
        Color blastTint = PSXImpactEffects.HullColor(Source.gameObject);
        Stage = BossDestructionStage.Burning;
        var ports = new[] { new Vector2(-.62f, .35f), new Vector2(.55f, -.4f), new Vector2(-.4f, -.52f), new Vector2(.55f, .5f), Vector2.zero };
        for (int beat = 0; beat < ports.Length; beat++)
        {
            Vector3 p = bounds.center + new Vector3(ports[beat].x * bounds.extents.x, ports[beat].y * bounds.extents.y, -.2f);
            float size = Mathf.Clamp(Mathf.Max(bounds.size.x, bounds.size.y) * (.18f + beat * .025f), .45f, 1.25f);
            PSXFireballs.Spawn(p, size, beat == 4 ? 5 : 3, size * .28f, .065f, blastTint);
            PSXImpactEffects.Burst(p, LevelRun.Current == LevelId.Underwater ? new Color(.35f, .65f, .7f) : Color.Lerp(new Color(1, .58f, .22f), blastTint, .35f));
            PreBlasts++;
            yield return Beat(.16f + beat * .015f);
        }
        Stage = BossDestructionStage.Breakup;
        ShatteredAt = Time.unscaledTime;
        BGMManager.StopBGM();
        SoundManager.PlaySound("bossDeath", .12f);
        if (Wreckage != null)
            Wreckage.ReleasePrepared();
        if (Source != null)
        {
            foreach (var hull in hulls)
                if (hull != null)
                    hull.enabled = false;
            float size = Mathf.Clamp(Mathf.Max(bounds.size.x, bounds.size.y) * .85f, 1.8f, 4.5f);
            PSXImpactEffects.Explosion(bounds.center, size);
            PSXFireballs.Shockwave(bounds.center, Mathf.Min(5, size * 1.05f), .7f, LevelRun.Current == LevelId.Underwater ? new Color(.3f, .62f, .68f) : new Color(1, .66f, .3f), 2, .26f);
            Camera.WhiteScreenJitter(.22f);
            completed?.Invoke(bounds.center);
        }
        yield return Beat(.8f);
        Stage = BossDestructionStage.Complete;
        Destroy(gameObject);
    }

    IEnumerator Beat(float seconds)
    {
        // Menu pause suspends the score as well as the half-second hold.
        float elapsed = 0;
        while (elapsed < seconds)
        {
            yield return null;
            if (!Menu.GameplayPaused)
                elapsed += Time.unscaledDeltaTime;
        }
    }

    void LateUpdate()
    {
        if (GameOverState.Active || (Source == null && (int)Stage < (int)BossDestructionStage.Breakup))
            Destroy(gameObject);
    }

    void OnDestroy()
    {
        if ((int)Stage < (int)BossDestructionStage.Breakup && Wreckage != null)
            Destroy(Wreckage.gameObject);
        if (Current == this)
            Current = null;
    }
}
