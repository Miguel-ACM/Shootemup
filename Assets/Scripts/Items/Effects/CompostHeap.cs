using System.Collections.Generic;
using UnityEngine;

// Compost: a kill leaves a small cloud of jade fumes where the hull fell; an enemy flying
// through it is poisoned. A kill that was already poisoned leaves a bigger, longer heap.
// Pooled like Hot Sauce's fire trail; each heap poisons a given enemy once.
public sealed class CompostHeap : MonoBehaviour
{
    const int Cap = 12;
    static int live;
    static readonly Stack<CompostHeap> pool = new Stack<CompostHeap>();
    readonly HashSet<AEnemy> touched = new HashSet<AEnemy>();
    ParticleSystem fumes;
    float age, next, duration, radius;
    AEnemy source;

    public static void Leave(AEnemy dead, int copies)
    {
        if (live >= Cap || dead == null)
            return;
        bool rotten = dead.GetComponent<PoisonedHull>() != null;
        while (pool.Count > 0 && pool.Peek() == null)
            pool.Pop();
        var heap = pool.Count > 0 ? pool.Pop() : Create();
        heap.source = dead;
        heap.age = heap.next = 0;
        heap.radius = (rotten ? .95f : .6f) + .12f * (copies - 1);
        heap.duration = (rotten ? 4 : 2.4f) + .5f * (copies - 1);
        heap.touched.Clear();
        heap.transform.position = new Vector3(dead.transform.position.x, dead.transform.position.y, 0);
        heap.gameObject.SetActive(true);
        heap.fumes.Play();
        RunInventory.Ping(UpgradeKind.Compost);
        ItemProcVfx.Emit(ItemProcShape.Toxic, heap.transform.position, PlayerShotPalette.Poison, heap.radius * .8f);
    }

    static CompostHeap Create()
    {
        var go = new GameObject("Compost heap");
        var heap = go.AddComponent<CompostHeap>();
        heap.fumes = PSXImpactEffects.Create(go.transform, "Jade fumes", 28, false);
        var fade = heap.fumes.colorOverLifetime;
        var gradient = new Gradient();
        var jade = PlayerShotPalette.Poison;
        gradient.SetKeys(new[] { new GradientColorKey(Color.Lerp(jade, Color.white, .2f), 0), new GradientColorKey(jade * .7f, 1) },
            new[] { new GradientAlphaKey(.55f, 0), new GradientAlphaKey(0, 1) });
        fade.color = gradient;
        return heap;
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
        if (Time.deltaTime <= 0 || WorldTimeStop.Frozen)
            return;
        if (GameRules.playerDisabled)
        {
            Release();
            return;
        }
        age += Time.deltaTime;
        if (age > duration)
        {
            Release();
            return;
        }
        if (age < next)
            return;
        next = age + .1f;
        var at = transform.position;
        // Slow jade puffs rising from the heap, thinning as it runs out.
        float strength = 1 - age / duration;
        var puff = new ParticleSystem.EmitParams
        {
            position = at + (Vector3)(Random.insideUnitCircle * radius * .75f) + new Vector3(0, 0, -1.5f),
            velocity = new Vector3(Random.Range(-.15f, .15f), .35f, 0),
            startLifetime = .9f,
            startSize3D = Vector3.one * Random.Range(.16f, .3f) * (.6f + .4f * strength),
            startColor = Color.white,
        };
        fumes.Emit(puff, 1);
        float dose = GameRules.playerShootDamage * 3;
        foreach (var target in AEnemy.Live)
        {
            if (target == null || target == source || !target.CanBeTargeted || touched.Contains(target))
                continue;
            target.ClosestDamagePoint(at, out Vector2 closest);
            if (((Vector2)at - closest).sqrMagnitude > radius * radius)
                continue;
            touched.Add(target);
            (target.GetComponent<PoisonedHull>() ?? target.gameObject.AddComponent<PoisonedHull>()).Infect(dose);
        }
    }

    void Release()
    {
        fumes.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        source = null;
        touched.Clear();
        gameObject.SetActive(false);
        pool.Push(this);
    }
}
