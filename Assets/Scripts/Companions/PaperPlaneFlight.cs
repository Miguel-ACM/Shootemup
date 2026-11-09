using UnityEngine;
using System.Collections.Generic;

// Paper Plane: perches beside the ship, then loops out through the nearest enemy and back.
// Every enemy it passes through takes a companion hit with the pilot's on-hit effects.
public sealed class PaperPlaneFlight : ACompanion
{
    enum Leg { Perch, Out, Back }
    const float Speed = 10, OutTurn = 6, BackTurn = 8, Reach = .3f, Range = 9, MaxOut = 1.6f;
    static readonly List<PaperPlaneFlight> live = new List<PaperPlaneFlight>();
    readonly HashSet<AEnemy> struck = new HashSet<AEnemy>();
    Shipcontroller owner;
    TrailRenderer wake;
    Vector3 worldPosition;
    Vector2 heading = Vector2.up;
    AEnemy target;
    Leg leg;
    float legTime, nextSortie;

    public static PaperPlaneFlight Attach(Shipcontroller ship)
    {
        var go = new GameObject("Paper plane");
        go.transform.SetParent(ship.transform, false);
        var plane = go.AddComponent<PaperPlaneFlight>();
        plane.owner = ship;
        plane.type = CompanionType.sides;
        go.transform.localScale = Vector3.one * (.42f / Mathf.Max(.01f, ship.transform.lossyScale.x));
        go.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.ExpansionItem(nameof(PaperPlane));
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = EverydayItemMaterials.For(nameof(PaperPlane));
        renderer.sortingLayerName = "Player";
        plane.wake = go.AddComponent<TrailRenderer>();
        plane.wake.sharedMaterial = CombatVisuals.ProjectileMaterial;
        plane.wake.time = .22f;
        plane.wake.startWidth = .05f;
        plane.wake.endWidth = 0;
        plane.wake.startColor = new Color(.85f, .92f, 1, .45f);
        plane.wake.endColor = new Color(.85f, .92f, 1, 0);
        plane.wake.sortingLayerName = "Player";
        plane.wake.sortingOrder = -4;
        plane.wake.emitting = false;
        plane.worldPosition = plane.Perch;
        plane.nextSortie = Time.time + .5f;
        return plane;
    }

    void OnEnable()
    {
        live.Add(this);
    }

    void OnDisable()
    {
        live.Remove(this);
    }

    Vector3 Perch
    {
        get
        {
            int index = Mathf.Max(0, live.IndexOf(this));
            float side = index % 2 == 0 ? 1 : -1;
            var p = owner.transform.position + new Vector3(side * (.75f + .3f * (index / 2)), -.25f + Mathf.Sin(Time.time * 2.3f + index) * .06f, -.1f);
            return p;
        }
    }

    void LateUpdate()
    {
        if (owner == null)
            return;
        float dt = Time.deltaTime;
        if (WorldTimeStop.Frozen || dt <= 0)
        {
            transform.position = worldPosition;
            return;
        }
        bool active = GameClock.ActivePlay && owner.health > 0;
        if (!active && leg != Leg.Perch)
            leg = Leg.Back;
        legTime += dt;
        switch (leg)
        {
            case Leg.Perch:
                worldPosition = Vector3.Lerp(worldPosition, Perch, 1 - Mathf.Exp(-dt * 12));
                heading = Vector3.RotateTowards(heading, Vector2.up, dt * BackTurn, 0);
                if (active && Time.time >= nextSortie)
                    Launch();
                break;
            case Leg.Out:
                bool alive = target != null && target.CanBeTargeted;
                if (alive)
                {
                    target.ClosestDamagePoint(worldPosition, out Vector2 aim);
                    heading = Vector3.RotateTowards(heading, (aim - (Vector2)worldPosition).normalized, dt * OutTurn, 0);
                }
                if (!alive || struck.Contains(target) || legTime > MaxOut)
                    TurnBack();
                Fly(dt);
                break;
            case Leg.Back:
                Vector2 home = Perch;
                heading = Vector3.RotateTowards(heading, (home - (Vector2)worldPosition).normalized, dt * BackTurn, 0);
                Fly(dt);
                if ((home - (Vector2)worldPosition).sqrMagnitude < .35f * .35f || legTime > 3)
                {
                    leg = Leg.Perch;
                    wake.emitting = false;
                    nextSortie = Time.time + .3f;
                }
                break;
        }
        transform.position = worldPosition;
        transform.rotation = Quaternion.LookRotation(Vector3.forward, heading) * Quaternion.Euler(0, Mathf.Sin(legTime * 9) * (leg == Leg.Perch ? 6 : 20), 0);
        if (active && leg != Leg.Perch)
            Strike();
    }

    void Launch()
    {
        target = CombatTargets.Nearest(owner.transform.position, null, Range);
        if (target == null)
        {
            nextSortie = Time.time + .2f;
            return;
        }
        struck.Clear();
        leg = Leg.Out;
        legTime = 0;
        wake.Clear();
        wake.emitting = true;
        heading = new Vector2(Mathf.Sign(worldPosition.x - owner.transform.position.x) * .35f, 1).normalized;
        ItemSound.Cue("fan", volume: .035f, pitch: 1.6f, cooldown: .3f);
    }

    void TurnBack()
    {
        leg = Leg.Back;
        legTime = 0;
        ItemProcVfx.Emit(ItemProcShape.Return, worldPosition, new Color(.55f, .72f, 1), .3f, heading);
    }

    void Fly(float dt) => worldPosition += (Vector3)(heading * Speed * dt);

    void Strike()
    {
        foreach (var enemy in AEnemy.Live)
        {
            if (enemy == null || !enemy.CanReceiveDamage || struck.Contains(enemy))
                continue;
            enemy.ClosestDamagePoint(worldPosition, out Vector2 point);
            if ((point - (Vector2)worldPosition).sqrMagnitude > Reach * Reach)
                continue;
            struck.Add(enemy);
            owner.PlayHitSound("hit1");
            CombatHit.Apply(enemy, GameRules.playerShootDamage * 2, point, heading, companion: true);
            return; // A kill can change the enemy set; resume next frame.
        }
    }
}
