using UnityEngine;

// An indestructible ball-lightning drone released by the Thunder boss. It moves
// on a grid inside a bounded box and leaves a dotted trail of stationary charge
// that fades after a few seconds: together the trails draw a temporary maze.
// It is not an enemy: player fire passes through it and it never collides.
public sealed class ThunderMazeSpark : MonoBehaviour
{
    Rect box;
    float cell, speed, life, trail, dropEvery, nextDrop, age;
    Vector2 target, direction;
    bool entering = true, leaving;
    Transform player, model;
    SpriteRenderer glow;

    public static ThunderMazeSpark Release(Vector3 from, Rect box, float cell, float life, float trail, float dropEvery, int seed)
    {
        var go = new GameObject("Indestructible maze spark");
        go.transform.position = new Vector3(from.x, from.y, -.4f);
        var spark = go.AddComponent<ThunderMazeSpark>();
        spark.box = box;
        spark.cell = cell;
        spark.life = life;
        spark.trail = trail;
        spark.dropEvery = dropEvery;
        spark.speed = cell / .3f;
        // Enter at a grid node on the upper edge of the box, spread by seed.
        int columns = Mathf.Max(1, Mathf.FloorToInt(box.width / cell));
        spark.target = spark.Snap(new Vector2(box.xMin + (seed * 3 % (columns + 1)) * cell, box.yMax));
        spark.direction = Vector2.down;
        var model = new GameObject("Spark drone");
        model.transform.SetParent(go.transform, false);
        model.transform.localScale = Vector3.one * .42f;
        model.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.CombatHull(10);
        var renderer = model.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = CombatMaterials.Fleet;
        renderer.sortingLayerName = "Enemy";
        spark.model = model.transform;
        spark.glow = ItemSpark.Sprite(go.transform, "Spark charge", "standard", LevelProjectilePalette.Accent(LevelRun.Current), .55f);
        spark.glow.transform.localPosition = new Vector3(0, 0, -.2f);
        var p = GameObject.FindGameObjectWithTag("Player");
        spark.player = p != null ? p.transform : null;
        return spark;
    }

    Vector2 Snap(Vector2 p)
    {
        float x = box.xMin + Mathf.Round((p.x - box.xMin) / cell) * cell;
        float y = box.yMin + Mathf.Round((p.y - box.yMin) / cell) * cell;
        return new Vector2(Mathf.Clamp(x, box.xMin, box.xMax), Mathf.Clamp(y, box.yMin, box.yMax));
    }

    void Update()
    {
        if (GameRules.playerDisabled || WorldTimeStop.Frozen)
            return;
        float dt = Time.deltaTime * LevelRun.EnemyTempoScale;
        age += dt;
        model.Rotate(0, 0, 540 * dt, Space.Self);
        glow.transform.localScale = Vector3.one * (.5f + .12f * Mathf.Sin(age * 30));
        if (leaving)
        {
            transform.position += Vector3.up * dt * 9;
            if (transform.position.y > Camera.UpLimit + 2)
                Destroy(gameObject);
            return;
        }
        Vector2 p = transform.position;
        p = Vector2.MoveTowards(p, target, speed * dt);
        transform.position = new Vector3(p.x, p.y, -.4f);
        if (!entering && age >= nextDrop)
        {
            nextDrop = age + dropEvery;
            if (player == null || Vector2.Distance(player.position, p) > 1.25f)
                CombatRound.Launch<SimpleRound>(p, direction, 0, .17f).FadeAt(trail);
        }
        if ((p - target).sqrMagnitude > .0001f)
            return;
        entering = false;
        if (age > life)
        {
            leaving = true;
            return;
        }
        Turn();
    }

    // Manhattan steps: never reverse, never leave the box, prefer not to box in the player.
    void Turn()
    {
        Vector2[] options = { Vector2.left, Vector2.right, Vector2.up, Vector2.down };
        Vector2 best = -direction;
        float bestScore = float.MinValue;
        foreach (var option in options)
        {
            if (option == -direction)
                continue;
            var next = target + option * cell;
            if (next.x < box.xMin - .01f || next.x > box.xMax + .01f || next.y < box.yMin - .01f || next.y > box.yMax + .01f)
                continue;
            float score = Random.value;
            if (player != null)
                score += Mathf.Min(2, Vector2.Distance(next, player.position)) * .35f;
            if (option == direction)
                score += .25f;
            if (score > bestScore)
            {
                bestScore = score;
                best = option;
            }
        }
        direction = best;
        target += best * cell;
    }

    public void Dismiss()
    {
        leaving = true;
    }
}
