using UnityEngine;
using System.Collections.Generic;

public sealed class SpectralDecoy : MonoBehaviour
{
    public static SpectralDecoy Current
    {
        get; private set;
    }

    static Material material;
    Shipcontroller owner;
    float age;
    readonly List<MeshRenderer> parts = new List<MeshRenderer>();
    MaterialPropertyBlock properties;
    void Awake()
    {
        properties = new MaterialPropertyBlock();
    }

    public static Vector3 AimPosition(Vector3 fallback) => Current != null && Current.owner != null && !GameRules.playerDisabled ? Current.transform.position : fallback;
    public static void Launch(Shipcontroller ship)
    {
        if (Current != null)
            Destroy(Current.gameObject);
        var go = new GameObject("Projected ship decoy");
        go.transform.position = ship.transform.position;
        var copy = go.AddComponent<SpectralDecoy>();
        copy.owner = ship;
        Current = copy;
        if (material == null)
            material = new Material(Resources.Load<Shader>("ShipDecoy")) { name = "Spectral ship projection" };
        var source = ship.GetComponent<PSXShipVisual>().Model;
        foreach (var mesh in source.GetComponentsInChildren<MeshFilter>())
        {
            if (mesh.sharedMesh == null)
                continue;
            var part = new GameObject(mesh.name);
            part.transform.SetParent(go.transform, false);
            part.transform.position = mesh.transform.position;
            part.transform.rotation = mesh.transform.rotation;
            part.transform.localScale = mesh.transform.lossyScale;
            part.AddComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
            var renderer = part.AddComponent<MeshRenderer>();
            var materials = new Material[mesh.sharedMesh.subMeshCount];
            for (int i = 0; i < materials.Length; i++)
                materials[i] = material;
            renderer.sharedMaterials = materials;
            renderer.sortingLayerName = "Player";
            renderer.sortingOrder = 0;
            copy.parts.Add(renderer);
        }
        ItemSound.Play("decoy", .2f);
    }

    void Update()
    {
        if (owner == null || GameRules.playerDisabled)
        {
            Destroy(gameObject);
            return;
        }
        if (WorldTimeStop.Frozen || Time.deltaTime <= 0)
            return;
        age += Time.deltaTime;
        if (age >= 3)
        {
            Destroy(gameObject);
            return;
        }
        if (age < .9f)
        {
            var camera = UnityEngine.Camera.main;
            float top = camera.transform.position.y + camera.orthographicSize - 1;
            var p = transform.position;
            p.y = Mathf.Min(top, p.y + Time.deltaTime * 5);
            transform.position = p;
        }
        properties.SetFloat("_Fade", Mathf.Min(1, (3 - age) / .4f));
        properties.SetFloat("_Phase", age);
        foreach (var part in parts)
            part.SetPropertyBlock(properties);
    }

    void OnDestroy()
    {
        if (Current == this)
            Current = null;
    }
}
