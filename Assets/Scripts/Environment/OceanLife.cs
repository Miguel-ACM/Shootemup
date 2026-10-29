using UnityEngine;

// Two interacting boid schools and a ray. Fixed buffers, a bounded 10 Hz steering
// solve, and smooth frame integration; no combat physics or per-fish allocation.
public sealed class OceanLife : MonoBehaviour
{
    sealed class Fish
    {
        public Transform model, shadow, tail;
        public Vector2 position, velocity, force;
        public float phase, size;
        public int school;
    }

    Fish[] fish;
    Material ink, shadowInk;
    float clock, nextSteer;
    void Start()
    {
        ink = new Material(CombatMaterials.Fleet);
        ink.SetColor("_Tint", new Color(.38f, .61f, .62f, .65f));
        shadowInk = new Material(CombatMaterials.Fleet);
        shadowInk.SetColor("_Tint", new Color(.035f, .09f, .12f, .24f));
        fish = new Fish[33];
        var random = new System.Random(LevelRun.Seed + 7713);
        float R(float a, float b) => Mathf.Lerp(a, b, (float)random.NextDouble());
        for (int i = 0; i < fish.Length; i++)
        {
            bool ray = i == fish.Length - 1;
            int school = i < 16 ? 0 : 1;
            var mesh = IndustrialMeshFactory.OceanAnimal(ray);
            var model = Add(ray ? "Swimming ray" : "Schooling reef fish", mesh, ink, -32);
            var shadow = Add("Seabed swimming shadow", mesh, shadowInk, -44);
            Transform tail = null;
            if (!ray)
            {
                tail = Add("Articulated swimming tail", IndustrialMeshFactory.OceanFishTail(), ink, -32);
                tail.SetParent(model, false);
                tail.localPosition = new Vector3(0, .27f, 0);
            }
            fish[i] = new Fish { model = model, shadow = shadow, tail = tail, position = new Vector2((school == 0 ? -3 : 2.7f) + R(-1.4f, 1.4f), R(-7, 7)), velocity = new Vector2(school == 0 ? 1 : -1, R(-.35f, .35f)), phase = R(0, 6.28f), size = ray ? .78f : R(.2f, .29f), school = ray ? 2 : school };
        }
    }

    Transform Add(string label, Mesh mesh, Material material, int order)
    {
        var go = new GameObject(label);
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = order;
        return go.transform;
    }

    void Steer()
    {
        for (int i = 0; i < fish.Length; i++)
        {
            var f = fish[i];
            Vector2 center = Vector2.zero, align = Vector2.zero, separate = Vector2.zero;
            int neighbors = 0;
            for (int j = 0; j < fish.Length; j++)
            {
                if (i == j)
                    continue;
                var other = fish[j];
                Vector2 delta = f.position - other.position;
                float d = delta.sqrMagnitude;
                if (d < .001f)
                    continue;
                if (f.school == other.school && d < 5.5f)
                {
                    center += other.position;
                    align += other.velocity;
                    neighbors++;
                }
                float space = other.school == 2 ? 2.8f : .40f;
                if (d < space)
                    separate += delta / Mathf.Max(.08f, d);
            }
            float phase = f.school == 0 ? 0 : 2.5f;
            Vector2 goal = f.school == 2 ? new Vector2(Mathf.Sin(clock * .11f) * 6, Mathf.Cos(clock * .13f) * 6) : new Vector2(Mathf.Sin(clock * .19f + phase) * 5.2f, Mathf.Cos(clock * .14f + phase) * 6.7f);
            Vector2 force = (goal - f.position) * .16f + separate * .75f;
            if (neighbors > 0)
                force += (center / neighbors - f.position) * .2f + (align / neighbors - f.velocity) * .6f;
            if (Mathf.Abs(f.position.x) > 7)
                force.x -= Mathf.Sign(f.position.x) * (Mathf.Abs(f.position.x) - 7) * 1.8f;
            if (Mathf.Abs(f.position.y) > 9)
                force.y -= Mathf.Sign(f.position.y) * (Mathf.Abs(f.position.y) - 9) * 1.5f;
            f.force = Vector2.ClampMagnitude(force, f.school == 2 ? .6f : 1.5f);
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        clock += dt;
        if (fish == null)
            return;
        if (clock >= nextSteer)
        {
            nextSteer = clock + .1f;
            Steer();
        }
        foreach (var f in fish)
        {
            f.velocity += f.force * dt;
            float speed = f.velocity.magnitude, min = f.school == 2 ? .35f : .65f, max = f.school == 2 ? .7f : 1.4f;
            f.velocity = f.velocity.normalized * Mathf.Clamp(speed, min, max);
            f.position += f.velocity * dt;
            float swim = clock * (f.school == 2 ? 2.2f : 9) + f.phase;
            var p = new Vector3(f.position.x, f.position.y, 68);
            f.model.localPosition = p;
            var target = Quaternion.FromToRotation(Vector3.down, f.velocity) * Quaternion.Euler(0, Mathf.Sin(swim) * (f.school == 2 ? 13 : 5), 0);
            f.model.localRotation = Quaternion.Slerp(f.model.localRotation, target, 1 - Mathf.Exp(-dt * 7));
            f.model.localScale = Vector3.one * f.size;
            if (f.tail != null)
                f.tail.localRotation = Quaternion.Euler(0, Mathf.Sin(swim) * 24, Mathf.Sin(swim) * 9);
            f.shadow.localPosition = p + new Vector3(.32f, -.46f, 14);
            f.shadow.localRotation = f.model.localRotation;
            f.shadow.localScale = new Vector3(f.size * 1.13f, f.size * 1.13f, .04f);
        }
    }

    void OnDestroy()
    {
        if (ink != null)
            Destroy(ink);
        if (shadowInk != null)
            Destroy(shadowInk);
    }
}

public static partial class IndustrialMeshFactory
{
    static Mesh oceanFishTail;
    public static Mesh OceanFishTail()
    {
        if (oceanFishTail != null)
            return oceanFishTail;
        var b = new Builder();
        b.Prism(new[] { new Vector2(0, 0), new Vector2(-.34f, .56f), new Vector2(0, .43f), new Vector2(.34f, .56f) }, .035f, 13, Vector3.zero, 6);
        return oceanFishTail = b.Finish("Articulated reef fish tail fin");
    }
}
