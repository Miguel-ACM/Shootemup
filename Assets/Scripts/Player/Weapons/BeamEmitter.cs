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
    readonly List<PlasmaOrb> plasmaTargets = new List<PlasmaOrb>();
    LaserSound sound;
    Coroutine firing;
    bool releasing;
    Vector3 attraction;
    bool hasAttraction;
    float TickInterval => .12f / Mathf.Max(.1f, GameRules.playerShootSpeed / 10f * Affinity.RateFactor * MagnifyingLens.Rate * ActiveRate);
    // Damage carries the beam; at most six beams, so extra shots help a little less.
    protected override WeaponAffinity Affinity => new WeaponAffinity(1.15f, 1.25f, .8f);

    void Awake()
    {
        shootSpeedModifier = .12f;
    }

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

    void OnDisable()
    {
        StopFiring();
    }

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
        bool bends = GameRules.playerShootFollowness > 0 && hasAttraction;
        Vector3 target = new Vector3(Mathf.Lerp(origin.x, attraction.x, Mathf.Clamp01(GameRules.playerShootFollowness)), attraction.y, origin.z);
        float distance = Vector2.Distance(origin, target);
        int approach = Mathf.Clamp(Mathf.CeilToInt(distance / .4f), 4, Samples - 6);
        Vector3 control = origin + Vector3.up * Mathf.Max(.25f, distance * .65f);
        Vector3 exit = (target - control).normalized;
        for (int step = 1; step < path.Length; step++)
        {
            if (!bends)
                path[step] = origin + Vector3.up * (step * .5f);
            else if (step <= approach)
            {
                float t = step / (float)approach;
                path[step] = (1 - t) * (1 - t) * origin + 2 * (1 - t) * t * control + t * t * target;
            }
            else
                path[step] = target + exit * ((step - approach) * .5f);
        }
    }

    int CountBeamContacts(Collider2D collider, float radius, out Vector2 point, out Vector2 direction)
    {
        point = transform.position;
        direction = Vector2.up;
        if (collider == null || !collider.enabled || paths == null)
            return 0;
        var owner = collider.GetComponent<AEnemy>();
        int contacts = 0;
        foreach (var path in paths)
            for (int step = 1; step < path.Length; step++)
            {
                Vector2 hit;
                bool touches = owner != null
                    ? BeamContact.Touches(owner, path[step - 1], path[step], radius, out hit)
                    : BeamContact.Touches(collider, path[step - 1], path[step], radius, out hit);
                if (touches)
                {
                    if (contacts == 0)
                    {
                        point = hit;
                        direction = (path[step] - path[step - 1]).normalized;
                    }
                    contacts++;
                    break; // One hit per beam, regardless of how many sampled segments overlap.
                }
            }
        return contacts;
    }

    void DamageTick(float width, float charge)
    {
        float damage = GameRules.playerShootDamage * 1.35f * charge * MagnifyingLens.Power * DamageMultiplier *
            Affinity.DamageFactor(GameRules.playerShootNumber <= 6 ? 1 : 0);
        targets.Clear();
        targets.AddRange(AEnemy.Live);
        foreach (var enemy in targets)
        {
            if (enemy == null || !enemy.CanBeTargeted) continue;
            int contacts = CountBeamContacts(enemy.GetComponent<Collider2D>(), width * .5f, out var point, out var direction);
            if (contacts > 0) CombatHit.Apply(enemy, damage * contacts, point, direction, energy: true);
        }
        // Snapshot before damaging: a destroyed orb returns to its pool and leaves Live.
        plasmaTargets.Clear();
        foreach (var round in CombatRound.Live)
            if (round is PlasmaOrb orb) plasmaTargets.Add(orb);
        foreach (var orb in plasmaTargets)
        {
            if (orb == null) continue;
            int contacts = CountBeamContacts(orb.GetComponent<Collider2D>(), width * .5f, out _, out _);
            if (contacts > 0) orb.Hit(damage * contacts);
        }
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
            float dt = GameClock.PlayerDelta;
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
            bool active = t >= warning && !releasing && SimulatorFlight.CanFire(transform.position);
            if (active && t >= nextVolley)
            {
                charge = CombatUpgrades.Current != null ? CombatUpgrades.Current.Volley() : 1;
                nextVolley = t + .65f;
            }
            float power = releasing ? Mathf.Pow(1 - releasedFor / .18f, 2) : active ? Mathf.Clamp01((t - warning) / .055f) : 0;
            sound.Drive(power, active);
            var aimOrigin = transform.position + Vector3.up;
            var target = GameRules.playerShootFollowness > 0
                ? CombatTargets.MarkedInRange(aimOrigin, 20) ?? CombatUpgrades.Nearest(aimOrigin, null, 20) : null;
            if (target != null)
            {
                Vector3 point = CombatTargets.AimPoint(aimOrigin,target);
                attraction = hasAttraction ? Vector3.Lerp(attraction, point, 1 - Mathf.Exp(-12 * dt)) : point;
                hasAttraction = true;
            }
            else
                hasAttraction = false;
            float lensSize = Mathf.Lerp(1, 1.6f, MagnifyingLens.Current != null ? MagnifyingLens.Current.Charge : 0);
            float width = .14f * Mathf.Sqrt(GameRules.playerShootSize) * lensSize;
            for (int i = 0; i < count; i++)
            {
                Trace(transform.position + spawnPoints[i], paths[i]);
                CombatWalls.ClipPath(paths[i], width * .5f);
                beams[i].DrawPath(paths[i], width, Mathf.Clamp01(t / warning), power, releasedFor / .18f);
            }
            if (active && t >= nextTick)
            {
                nextTick = t + TickInterval;
                DamageTick(width, charge);
            }
            yield return null;
        }
        Cleanup();
        firing = null;
    }
}
