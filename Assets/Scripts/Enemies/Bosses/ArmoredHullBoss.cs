using System.Collections;
using UnityEngine;

// Keep the root damage collider aligned with the banked mesh and its gun ports.
public abstract class ArmoredHullBoss : DistrictBoss
{
    protected abstract Vector2[] Outline { get; }
    PolygonCollider2D hitbox;
    Vector2[] points;

    protected override void Start()
    {
        base.Start();
        var circle = GetComponent<CircleCollider2D>();
        circle.enabled = false;
        Destroy(circle);
        hitbox = gameObject.AddComponent<PolygonCollider2D>();
        hitbox.isTrigger = true;
        points = new Vector2[Outline.Length];
        LateUpdate();
    }

    void LateUpdate()
    {
        for (int i = 0; i < points.Length; i++)
            points[i] = transform.InverseTransformPoint(Hull.TransformPoint(Outline[i]));
        hitbox.SetPath(0, points);
    }

    protected override void Attack()
    {
        if (openingComplete)
            OpenCards();
    }

    protected IEnumerator PrimePair(float seconds)
    {
        for (float t = 0; t < seconds; t += Holding ? 0 : Time.deltaTime * LevelRun.EnemyTempoScale)
        {
            Cue(Muzzle(1), seconds - t);
            Cue(Muzzle(2), seconds - t);
            yield return null;
        }
    }

    protected float Lane(float offset) => Mathf.Lerp(Camera.LeftLimit + 1.85f, Camera.RightLimit - 1.85f, (offset + 1) * .5f);
}
