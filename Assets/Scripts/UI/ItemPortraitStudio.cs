using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Renders a list of item models into one atlas, far away from the playfield, so menus can show
/// the actual 3D equipment. Each item is a cell; <see cref="Cell"/> gives its uvRect.
/// </summary>
public sealed class ItemPortraitStudio : MonoBehaviour
{
    const int CellPixels = 96;
    const float CellUnits = 1.3f;
    static readonly Vector3 Origin = new Vector3(-900, 0, 0);
    readonly List<Transform> models = new List<Transform>();
    RenderTexture atlas;
    int columns, rows;
    public Texture Atlas => atlas;

    public static ItemPortraitStudio Create(IList<string> kinds)
    {
        var studio = new GameObject("Item portrait studio").AddComponent<ItemPortraitStudio>();
        studio.Build(kinds);
        return studio;
    }

    void Build(IList<string> kinds)
    {
        transform.position = Origin;
        columns = Mathf.Max(1, Mathf.Min(10, kinds.Count));
        rows = Mathf.Max(1, Mathf.CeilToInt(kinds.Count / (float)columns));
        atlas = new RenderTexture(columns * CellPixels, rows * CellPixels, 16, RenderTextureFormat.ARGB32) { name = "Item portraits", filterMode = FilterMode.Point };
        var go = new GameObject("Item portrait camera");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0, 0, -10);
        var camera = go.AddComponent<UnityEngine.Camera>();
        camera.orthographic = true;
        camera.orthographicSize = rows * CellUnits * .5f;
        camera.aspect = columns / (float)rows;
        camera.nearClipPlane = .1f;
        camera.farClipPlane = 20;
        camera.backgroundColor = Color.clear;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.targetTexture = atlas;
        camera.depth = -48;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        for (int i = 0; i < kinds.Count; i++)
        {
            var mesh = IndustrialMeshFactory.ExpansionItem(kinds[i]);
            var model = new GameObject(kinds[i] + " portrait");
            model.transform.SetParent(transform, false);
            model.transform.localPosition = new Vector3((i % columns - (columns - 1) * .5f) * CellUnits, ((rows - 1) * .5f - i / columns) * CellUnits, 0);
            float size = mesh != null ? Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.y, mesh.bounds.size.z) : 1;
            model.transform.localScale = Vector3.one * (.9f / Mathf.Max(size, .01f));
            model.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = model.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = EverydayItemMaterials.For(kinds[i]);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            models.Add(model.transform);
        }
    }

    public Rect Cell(int index) => new Rect(index % columns / (float)columns, 1 - (index / columns + 1) / (float)rows, 1f / columns, 1f / rows);

    void LateUpdate()
    {
        // Stepped turntable, each model out of phase, matching the collected-item column.
        float t = Mathf.Floor(Time.unscaledTime * 15) / 15;
        for (int i = 0; i < models.Count; i++)
            models[i].localRotation = Quaternion.Euler(-18, t * 50 + i * 37, 8);
    }

    void OnDestroy()
    {
        if (atlas != null)
        {
            atlas.Release();
            Destroy(atlas);
        }
    }
}
