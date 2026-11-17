using System.Collections.Generic;
using UnityEngine;

// Purple clouds travel with the air. The pilot's cockpit centre is the status contact.
public sealed class SporeCloud : MonoBehaviour
{
    const float Radius = 1.47f;
    const float FallSpeed = 1.26f;
    static readonly List<SporeCloud> clouds = new List<SporeCloud>(3);
    static readonly int Clock = Shader.PropertyToID("_Clock"), Strength = Shader.PropertyToID("_Strength");
    static Material ink;
    static Mesh quad;
    MaterialPropertyBlock properties;
    MeshRenderer visual;
    Vector3 origin;
    float age, phase, fade = 1;
    bool fading;
    public static int Count => clouds.Count;

    public static void Dissipate()
    {
        foreach (var cloud in clouds)
            cloud.fading = true;
    }

    internal static void Warm()
    {
        if (quad == null)
        {
            quad = new Mesh { name = "Shared spore cloud sheet" };
            quad.vertices = new[] { new Vector3(-1,-1), new Vector3(-1,1), new Vector3(1,1), new Vector3(1,-1) };
            quad.triangles = new[] { 0,1,2, 0,2,3 };
            quad.RecalculateBounds();
        }
        if (ink == null)
            ink = new Material(Resources.Load<Shader>("SporeCloud"));
    }

    public static SporeCloud Drop(Vector3 source, float phase)
    {
        if (clouds.Count >= 3 || GameRules.playerDisabled)
            return null;
        Warm();
        var cloud = new GameObject("Falling purple spore cloud").AddComponent<SporeCloud>();
        cloud.origin = new Vector3(Mathf.Clamp(source.x, Camera.LeftLimit + Radius, Camera.RightLimit - Radius), source.y, 4);
        cloud.phase = phase;
        cloud.transform.position = cloud.origin;
        cloud.transform.localScale = new Vector3(Radius * 1.23f, Radius, 1);
        cloud.gameObject.AddComponent<MeshFilter>().sharedMesh = quad;
        cloud.visual = cloud.gameObject.AddComponent<MeshRenderer>();
        cloud.visual.sharedMaterial = ink;
        cloud.visual.sortingLayerName = "Enemy";
        cloud.visual.sortingOrder = -5;
        cloud.properties = new MaterialPropertyBlock();
        cloud.Paint();
        clouds.Add(cloud);
        return cloud;
    }

    public static bool Confuses(Vector2 point)
    {
        if (LevelRun.Current != LevelId.Mushroom || !GameClock.ActivePlay)
            return false;
        foreach (var cloud in clouds)
        {
            if (cloud.fading || cloud.age < .3f)
                continue;
            Vector2 q = (point - (Vector2)cloud.transform.position) / Radius;
            q.x /= 1.23f;
            if (q.sqrMagnitude < .72f * .72f)
                return true;
        }
        return false;
    }

    void Paint()
    {
        properties.SetFloat(Clock, age + phase);
        properties.SetFloat(Strength, Mathf.Clamp01(age * 3) * fade);
        visual.SetPropertyBlock(properties);
    }

    void Update()
    {
        if (LevelRun.Current != LevelId.Mushroom || CloudTransition.Busy)
        {
            Destroy(gameObject);
            return;
        }
        if (WorldTimeStop.Frozen || Menu.GameplayPaused)
            return;
        fading |= GameRules.playerDisabled || CombatDecision.Active;
        float dt = Time.deltaTime;
        age += dt;
        if (fading)
            fade -= dt * 3;
        transform.position = origin + new Vector3(Mathf.Sin(phase + age * .75f) * .24f, -age * FallSpeed, 0);
        if (fade <= 0 || transform.position.y < Camera.DownLimit - Radius * 1.5f)
            Destroy(gameObject);
        else
            Paint();
    }

    void OnDestroy() => clouds.Remove(this);
}
