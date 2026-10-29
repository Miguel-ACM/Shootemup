using UnityEngine;

/// <summary>Connects a 3D visual to the existing 2D player's colour and movement.</summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class PSXShipVisual : MonoBehaviour
{
    public Transform Model => model;
    [SerializeField] SpriteRenderer sourceSprite;
    [SerializeField] Transform model;
    [SerializeField] MeshRenderer modelRenderer;
    [SerializeField] float bankDegrees = 28f;
    // PSXv2: orientation from the FBX axis check, extra part renderers and a non-banking hitbox.
    [SerializeField] bool customOrientation;
    [SerializeField] Quaternion baseRotation = Quaternion.identity;
    [SerializeField] MeshRenderer[] partRenderers;
    [SerializeField] Renderer[] hitboxRenderers;
    MaterialPropertyBlock properties;
    Vector3 previousPosition;
    float bank;
    public void ScaleHull(float factor)
    {
        if (model != null)
            model.localScale *= factor;
        if (hitboxRenderers != null)
            foreach (var marker in hitboxRenderers)
                if (marker != null)
                    marker.transform.localScale *= factor;
    }

    // Selectable ships swap the hull under the banking model; the authored hull stays as the fallback.
    GameObject alternateHull;
    MeshRenderer defaultRenderer;
    MeshRenderer[] defaultParts;
    bool defaultPoseCaptured;
    Matrix4x4 defaultPose;
    Quaternion RestRotation => customOrientation ? baseRotation : Quaternion.Euler(-75f, 0f, 0f) * Quaternion.Euler(0f, 180f, 0f);

    /// <summary>Shows a Terra hull (authored in ship-root space, nose +Y), or the authored hull for null or a missing model.</summary>
    public void UseHull(string terraModel)
    {
        CaptureDefault();
        if (alternateHull != null)
        {
            alternateHull.SetActive(false);
            Destroy(alternateHull);
            alternateHull = null;
        }
        var mesh = terraModel != null ? IndustrialMeshFactory.TerraModel(terraModel) : null;
        if (mesh == null)
        {
            modelRenderer = defaultRenderer;
            partRenderers = defaultParts;
            Apply();
            return;
        }
        foreach (var part in DefaultRenderers())
            part.enabled = false;
        alternateHull = BuildTerraHull(terraModel, mesh, model);
        var saved = model.localRotation;
        model.localRotation = RestRotation;
        alternateHull.transform.SetPositionAndRotation(transform.position, transform.rotation);
        alternateHull.transform.localScale = Vector3.one * (transform.lossyScale.x / model.lossyScale.x);
        model.localRotation = saved;
        partRenderers = alternateHull.GetComponentsInChildren<MeshRenderer>();
        modelRenderer = partRenderers[0];
        Apply();
    }

    /// <summary>A copy of a hull with the ship root's scale, for parking it in the hangar.</summary>
    public GameObject BuildDisplayHull(string terraModel)
    {
        CaptureDefault();
        var root = new GameObject("Parked " + (terraModel ?? "GR-07"));
        root.transform.localScale = transform.lossyScale;
        var mesh = terraModel != null ? IndustrialMeshFactory.TerraModel(terraModel) : null;
        if (mesh != null)
        {
            BuildTerraHull(terraModel, mesh, root.transform);
            return root;
        }
        var hull = new GameObject("GR-07 hull");
        hull.transform.SetParent(root.transform, false);
        hull.transform.localPosition = defaultPose.GetColumn(3);
        hull.transform.localRotation = defaultPose.rotation;
        hull.transform.localScale = defaultPose.lossyScale;
        hull.AddComponent<MeshFilter>().sharedMesh = defaultRenderer.GetComponent<MeshFilter>().sharedMesh;
        var renderer = hull.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = defaultRenderer.sharedMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return root;
    }

    GameObject BuildTerraHull(string name, Mesh mesh, Transform parent)
    {
        var hull = new GameObject("Terra " + name);
        hull.transform.SetParent(parent, false);
        hull.layer = gameObject.layer;
        hull.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = hull.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = IndustrialMeshFactory.TerraMaterial(name) ?? defaultRenderer.sharedMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingLayerID = defaultRenderer.sortingLayerID;
        renderer.sortingOrder = defaultRenderer.sortingOrder;
        IndustrialMeshFactory.AttachTerraParts(name, hull.transform, renderer.sharedMaterial);
        foreach (var child in hull.GetComponentsInChildren<Transform>())
            child.gameObject.layer = gameObject.layer;
        return hull;
    }

    MeshRenderer[] DefaultRenderers() => defaultParts != null && defaultParts.Length > 0 ? defaultParts : new[] { defaultRenderer };

    void CaptureDefault()
    {
        if (defaultPoseCaptured)
            return;
        defaultPoseCaptured = true;
        defaultRenderer = modelRenderer;
        defaultParts = partRenderers;
        var saved = model.localRotation;
        model.localRotation = RestRotation;
        defaultPose = transform.worldToLocalMatrix * defaultRenderer.transform.localToWorldMatrix;
        model.localRotation = saved;
    }

    static readonly int Tint = Shader.PropertyToID("_Tint");
    static readonly int PlayerDepth = Shader.PropertyToID("_PlayerDepth");

    public void Configure(SpriteRenderer sprite, Transform visual, MeshRenderer meshRenderer)
    {
        sourceSprite = sprite;
        model = visual;
        modelRenderer = meshRenderer;
        Apply();
    }

    public void Configure(SpriteRenderer sprite, Transform visual, MeshRenderer[] parts, Quaternion orientation, Renderer[] hitbox)
    {
        customOrientation = true;
        baseRotation = orientation;
        partRenderers = parts;
        hitboxRenderers = hitbox;
        Configure(sprite, visual, parts.Length > 0 ? parts[0] : null);
    }

    void OnEnable()
    {
        previousPosition = transform.position;

        Apply();
    }

    void LateUpdate()
    {
        if (model == null || modelRenderer == null || sourceSprite == null)
            return;
        if (Application.isPlaying && Time.deltaTime > 0f)
        {
            float speed = (transform.position.x - previousPosition.x) / Time.deltaTime;
            float target = Mathf.Clamp(-speed / 8f, -1f, 1f) * bankDegrees;
            bank = Mathf.Lerp(bank, target, 1f - Mathf.Exp(-12f * Time.deltaTime));
        }
        previousPosition = transform.position;
        // Bank around the nose (+Y in Ship space) so the hitbox, a sibling, never shifts.
        model.localRotation = customOrientation
            ? Quaternion.AngleAxis(bank + (GetComponent<ShipBarrelRoll>()?.Angle ?? 0) + PolarityEngine.SpinAngle, Vector3.up) * baseRotation
            // Face the imported nose toward +Y while keeping the dorsal side toward -Z.
            : Quaternion.Euler(-75f, 0f, 0f) * Quaternion.Euler(0f, 180f, 0f) * Quaternion.AngleAxis(bank + PolarityEngine.SpinAngle, Vector3.forward);
        Apply();
    }

    void Apply()
    {
        if (sourceSprite == null || modelRenderer == null)
            return;
        sourceSprite.enabled = false;
        // Companions use the Player layer at order 0. Draw every player part after them.
        sourceSprite.sortingOrder = Mathf.Max(1, sourceSprite.sortingOrder);
        modelRenderer.sortingLayerID = sourceSprite.sortingLayerID;
        modelRenderer.sortingOrder = sourceSprite.sortingOrder;
        if (properties == null)
            properties = new MaterialPropertyBlock();
        modelRenderer.GetPropertyBlock(properties);
        properties.SetColor(Tint, sourceSprite.color * PolarityEngine.ShipTint);
        properties.SetFloat(PlayerDepth, 1f);
        modelRenderer.SetPropertyBlock(properties);
        modelRenderer.enabled = sourceSprite.color.a > 0.001f;
        if (partRenderers != null)
            foreach (var part in partRenderers)
            {
                if (part == null || part == modelRenderer)
                    continue;
                part.sortingLayerID = modelRenderer.sortingLayerID;
                part.sortingOrder = modelRenderer.sortingOrder;
                part.SetPropertyBlock(properties);
                part.enabled = modelRenderer.enabled;
            }
        // The hitbox stays fully visible through invulnerability blinking; it only hides on death.
        if (hitboxRenderers != null)
            foreach (var marker in hitboxRenderers)
                if (marker != null)
                    marker.enabled = sourceSprite.color.a > 0.001f && !GameRules.playerDisabled;
    }
}
