using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[UnityEngine.Scripting.Preserve]
public sealed class BeamEmitter : AWeaponAutomatic
{
    const int Samples = 41;
    BeamRibbon[] beams;
    Vector3[][] paths;
    readonly List<AEnemy> targets = new List<AEnemy>();
    LaserSound sound;
    Coroutine firing;
    bool releasing;
    Vector3 attraction;
    bool hasAttraction;

    void Awake() { shootSpeedModifier = .12f; }

    public override void Shoot()
    {
        if (!KeepFiring() || firing != null)
            return;
        releasing = false;
        firing = StartCoroutine(Fire());
    }

    public override void WeaponPressed(bool primary)
    {
        if (releasing && firing != null)
            StopFiring();
        base.WeaponPressed(primary);
    }

    public override void StopFiring()
    {
        base.StopFiring();
        firing = null;
        Cleanup();
    }

    protected override void WeaponReleased(bool primary)
    {
        if (EffectiveInput == WeaponInput.both ? isStopingPrimary && isStopingSecondary :
            EffectiveInput == WeaponInput.primary ? primary : !primary)
            releasing = true;
    }

    void OnDisable() { StopFiring(); }

    void Cleanup()
    {
        if (beams != null)
            foreach (var beam in beams)
                if (beam != null)
                    Destroy(beam.gameObject);
        beams = null;
        paths = null;
        hasAttraction = false;
        sound?.Stop();
    }

    void Trace(Vector3 origin, Vector3[] path)
    {
        path[0] = origin;
        Vector2 direction = Vector2.up;
        float bend = Mathf.Min(3, GameRules.playerShootFollowness);
        for (int step = 1; step < path.Length; step++)
        {
            if (bend > 0 && hasAttraction)
            {
                Vector2 delta = attraction - path[step - 1];
                if (delta.sqrMagnitude > .04f)
                    direction = Vector3.RotateTowards(direction, delta.normalized, bend * .045f, 0);
            }
            path[step] = path[step - 1] + (Vector3)direction * .5f;
        }
    }

    bool Touches(AEnemy enemy, float radius, out Vector2 point, out Vector2 direction)
    {
        point = enemy.transform.position;
        direction = Vector2.up;
        var collider = enemy.GetComponent<Collider2D>();
        if (collider == null || !collider.enabled)
            return false;
        foreach (var path in paths)
            for (int step = 1; step < path.Length; step++)
            {
                Vector2 start = path[step - 1], segment = path[step] - path[step - 1];
                float lengthSquared = segment.sqrMagnitude;
                Vector2 probe = start + segment * Mathf.Clamp01(Vector2.Dot((Vector2)collider.bounds.center - start, segment) / lengthSquared);
                for (int i = 0; i < 3; i++)
                {
                    Vector2 surface = collider.ClosestPoint(probe);
                    probe = start + segment * Mathf.Clamp01(Vector2.Dot(surface - start, segment) / lengthSquared);
                }
                if ((collider.ClosestPoint(probe) - probe).sqrMagnitude <= radius * radius)
                {
                    point = probe;
                    direction = segment.normalized;
                    return true;
                }
            }
        return false;
    }

    IEnumerator Fire()
    {
        int count = Mathf.Min(6, spawnPoints.Length);
        beams = new BeamRibbon[count];
        paths = new Vector3[count][];
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Continuous cannon beam");
            go.transform.SetParent(transform, false);
            beams[i] = go.AddComponent<BeamRibbon>();
            beams[i].Build(true);
            paths[i] = new Vector3[Samples];
        }
        if (sound == null)
            sound = gameObject.AddComponent<LaserSound>();
        float charge = CombatUpgrades.Current != null ? CombatUpgrades.Current.Volley() : 1;
        float warning = .25f / Mathf.Max(1, GameRules.playerShootSpeed / 10);
        float t = 0, releasedFor = 0, nextTick = warning, nextVolley = warning + .65f;
        sound.Charge(warning, true);
        while (!GameRules.playerDisabled)
        {
            float dt = WorldTimeStop.PlayerDelta;
            if (Menu.GameplayPaused || dt <= 0)
            {
                yield return null;
                continue;
            }
            t += dt;
            bool held = EffectiveInput == WeaponInput.both ? !isStopingPrimary || !isStopingSecondary :
                EffectiveInput == WeaponInput.primary ? !isStopingPrimary : !isStopingSecondary;
            releasing |= !held;
            if (releasing)
                releasedFor += dt;
            if (releasedFor >= .18f)
                break;
            bool active = t >= warning && !releasing;
            if (active && t >= nextVolley)
            {
                charge = CombatUpgrades.Current != null ? CombatUpgrades.Current.Volley() : 1;
                nextVolley = t + .65f;
            }
            float power = releasing ? Mathf.Pow(1 - releasedFor / .18f, 2) : active ? Mathf.Clamp01((t - warning) / .055f) : 0;
            sound.Drive(power, active);
            var target = GameRules.playerShootFollowness > 0 ? CombatUpgrades.Nearest(transform.position + Vector3.up, null, 20) : null;
            if (target != null)
            {
                attraction = hasAttraction ? Vector3.Lerp(attraction, target.transform.position, 1 - Mathf.Exp(-12 * dt)) : target.transform.position;
                hasAttraction = true;
            }
            else
                hasAttraction = false;
            float lensSize = Mathf.Lerp(1, 1.6f, MagnifyingLens.Current != null ? MagnifyingLens.Current.Charge : 0);
            float width = .14f * Mathf.Sqrt(GameRules.playerShootSize) * lensSize;
            for (int i = 0; i < count; i++)
            {
                Trace(transform.position + spawnPoints[i], paths[i]);
                beams[i].DrawPath(paths[i], width, Mathf.Clamp01(t / warning), power, releasedFor / .18f);
            }
            if (active && t >= nextTick)
            {
                nextTick = t + .12f;
                targets.Clear();
                targets.AddRange(AEnemy.Live);
                foreach (var enemy in targets)
                {
                    if (enemy == null || !enemy.CanBeTargeted || !Touches(enemy, width * .5f, out var point, out var direction))
                        continue;
                    float amount = GameRules.playerShootDamage * 1.2f * charge * MagnifyingLens.Power;
                    float dealt = CombatUpgrades.Current != null ? CombatUpgrades.Current.DirectHit(enemy, amount) : amount;
                    enemy.HitAt(point, direction);
                    enemy.damage(dealt);
                    Score.IncreaseScore(Mathf.RoundToInt(dealt));
                }
            }
            yield return null;
        }
        Cleanup();
        firing = null;
    }
}
