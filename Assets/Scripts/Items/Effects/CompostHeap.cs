using System.Collections.Generic;
using UnityEngine;

// Compost: a kill leaves a small cloud of jade fumes where the hull fell; an enemy flying
// through it is poisoned. A kill that was already poisoned leaves a bigger, longer heap.
// Pooled like Hot Sauce's fire trail; each heap poisons a given enemy once. The fumes are one
// animated gas cloud (Toxic Cloud shader) instead of square particle puffs (Miguel, 15 November 2026).
public sealed class CompostHeap : MonoBehaviour
{
    const int Cap = 12;
    static int live;
    static readonly Stack<CompostHeap> pool = new Stack<CompostHeap>();
    readonly HashSet<AEnemy> touched = new HashSet<AEnemy>();
    static Material cloudMaterial;
    static Mesh quad;
    MeshRenderer cloud;
    MaterialPropertyBlock look;
    float age, next, duration, radius, seed;
    AEnemy source;

    public static void Leave(AEnemy dead, int copies)
    {
        if (live >= Cap || dead == null)
            return;
        bool rotten = dead.TryGetComponent<PoisonedHull>(out var poison) && poison.isActiveAndEnabled;
        while (pool.Count > 0 && pool.Peek() == null)
            pool.Pop();
        var heap = pool.Count > 0 ? pool.Pop() : Create();
        heap.source = dead;
        heap.age = heap.next = 0;
        heap.radius = (rotten ? .95f : .6f) + .12f * (copies - 1);
        heap.duration = (rotten ? 4 : 2.4f) + .5f * (copies - 1);
        heap.touched.Clear();
        heap.transform.position = new Vector3(dead.transform.position.x, dead.transform.position.y, 0);
        heap.seed = Random.Range(0f, 50f);
        // The opaque body reaches the poisoning radius.
        heap.cloud.transform.localScale = Vector3.one * heap.radius * 2.9f;
        heap.gameObject.SetActive(true);
        heap.Shade();
        RunInventory.Ping(UpgradeKind.Compost);
        ItemProcVfx.Emit(ItemProcShape.Toxic, heap.transform.position, PlayerShotPalette.Poison, heap.radius * .8f);
    }

    static CompostHeap Create()
    {
        var go = new GameObject("Compost heap");
        var heap = go.AddComponent<CompostHeap>();
        if (cloudMaterial == null)
            cloudMaterial = new Material(Resources.Load<Shader>("ToxicCloud")) { name = "Compost fumes" };
        if (quad == null)
        {
            quad = new Mesh { name = "Compost cloud quad" };
            quad.vertices = new[] { new Vector3(-.5f, -.5f), new Vector3(-.5f, .5f), new Vector3(.5f, .5f), new Vector3(.5f, -.5f) };
            quad.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            quad.RecalculateBounds();
        }
        var gas = new GameObject("Jade fumes");
        gas.transform.SetParent(go.transform, false);
        gas.AddComponent<MeshFilter>().sharedMesh = quad;
        heap.cloud = gas.AddComponent<MeshRenderer>();
        heap.cloud.sharedMaterial = cloudMaterial;
        // Under every ship and round.
        heap.cloud.sortingLayerName = "Enemy";
        heap.cloud.sortingOrder = -6;
        heap.look = new MaterialPropertyBlock();
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
        Shade();
        if (age < next)
            return;
        next = age + .1f;
        var at = transform.position;
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

    // The cloud swells in, thins as the heap runs out and fades at its end.
    void Shade()
    {
        look.SetFloat("_Life", Mathf.Clamp01(age / .3f) * Mathf.Clamp01((duration - age) / .6f));
        look.SetFloat("_Strength", 1 - age / Mathf.Max(.01f, duration));
        look.SetFloat("_Seed", seed);
        look.SetFloat("_Clock", Time.time);
        cloud.SetPropertyBlock(look);
    }

    void Release()
    {
        source = null;
        touched.Clear();
        gameObject.SetActive(false);
        pool.Push(this);
    }
}
