using System.Collections.Generic;
using UnityEngine;

// Offensive effects spend energy once and advance only during live combat.
public abstract class OffensiveActiveItem : AActiveItem
{
    public OffensiveCast Cast { get; private set; }
    protected virtual bool Repress() => false;
    protected abstract OffensiveCast Launch();
    protected bool HasWeapons
    {
        get
        {
            var loadout = s != null ? s.GetComponent<ShipLoadout>() : null;
            return loadout != null && (loadout.Weapons[0] != null || loadout.Weapons[1] != null);
        }
    }

    public sealed override float OnPress(float mana)
    {
        if (s == null || s.health <= 0 || !GameClock.ActivePlay)
            return -2;
        if (Cast != null)
            return Repress() ? mana : -2;
        float valid = CheckValidPress(mana);
        if (valid < 0) return valid;
        Cast = Launch();
        if (Cast == null) return -2;
        Cast.Bind(this, s);
        lastCD = 0;
        return mana - cost;
    }

    protected T CreateCast<T>(string title) where T : OffensiveCast
    {
        var go = new GameObject(title);
        go.transform.position = s.transform.position;
        return go.AddComponent<T>();
    }

    protected virtual void OnDestroy()
    {
        if (Cast != null) Destroy(Cast.gameObject);
    }
}

public abstract class OffensiveCast : MonoBehaviour
{
    public Shipcontroller Owner { get; private set; }
    protected OffensiveActiveItem Source { get; private set; }
    public float Age { get; private set; }
    protected readonly List<AEnemy> Targets = new List<AEnemy>(48);

    public void Bind(OffensiveActiveItem source, Shipcontroller owner)
    {
        Source = source;
        Owner = owner;
        Begin();
    }

    protected virtual void Begin() { }
    protected abstract void Step(float dt);
    protected void Finish() { enabled = false; Destroy(gameObject); }

    protected virtual void Update()
    {
        if (Owner == null || Owner.health <= 0 || Source == null || GameOverState.Active)
        {
            Finish();
            return;
        }
        float dt = GameClock.ActivePlay ? GameClock.PlayerDelta : 0;
        if (dt <= 0) return;
        Age += dt;
        Step(dt);
    }

    protected void SnapshotTargets()
    {
        Targets.Clear();
        foreach (var enemy in AEnemy.Live)
            if (enemy != null && enemy.CanBeTargeted && enemy.CanReceiveDamage &&
                enemy.transform.position.y <= Camera.UpLimit + .5f && enemy.transform.position.y >= Camera.DownLimit - .5f)
                Targets.Add(enemy);
        Targets.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
    }

    protected Transform Model(string kind, float width)
    {
        var go = new GameObject(kind + " deployed model");
        go.transform.SetParent(transform, false);
        var mesh = IndustrialMeshFactory.ExpansionItem(kind);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = EverydayItemMaterials.For(kind);
        renderer.sortingLayerName = "Bullet";
        renderer.sortingOrder = 15;
        go.transform.localScale = Vector3.one * (width / Mathf.Max(.01f, mesh.bounds.size.x));
        return go.transform;
    }

    protected PlayerRound Shot(Vector3 from, Vector2 heading, float power, float speed, PlayerDelivery delivery)
    {
        // Secondary generation keeps coatings but prevents recursive procs/charge farming.
        var round = PlayerRound.Spawn(from, heading, GameRules.playerShootDamage * power, speed, delivery, 1);
        if (round != null) round.returns = false;
        return round;
    }

    protected static bool Contact(AEnemy enemy, Vector2 from, Vector2 to, float radius, out Vector2 point)
    {
        return BeamContact.Touches(enemy, from, to, radius, out point);
    }
}
