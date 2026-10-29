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
    public GameObject BuildDisplayHull(string terraModel, Color cockpitColor)
    {
        CaptureDefault();
        var root = new GameObject("Parked " + (terraModel ?? "GR-07"));
        root.transform.localScale = transform.lossyScale;
        BuildCockpit(root.transform, cockpitColor);
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

    // Every ship wears a glowing hexagonal cockpit over the hitbox, so the point that can be hit is always clear.
    static readonly Vector3 CockpitCentre = new Vector3(0, .08f, -.2f);
    const float CockpitRadius = .062f;
    Transform cockpitFrame;
    MeshRenderer cockpit;

    public void UseCockpit(Color color)
    {
        CaptureDefault();
        if (cockpitFrame == null)
        {
            cockpitFrame = new GameObject("Cockpit frame").transform;
            cockpitFrame.SetParent(model, false);
            var saved = model.localRotation;
            model.localRotation = RestRotation;
            cockpitFrame.SetPositionAndRotation(transform.position, transform.rotation);
            cockpitFrame.localScale = Vector3.one * (transform.lossyScale.x / model.lossyScale.x);
            model.localRotation = saved;
        }
        if (cockpit != null)
            DestroyCockpit(cockpit);
        cockpit = BuildCockpit(cockpitFrame, color);
        cockpit.gameObject.layer = gameObject.layer;
        Apply();
    }

    /// <summary>A shining hexagonal canopy: dark rim, coloured glass and a bright crown, unlit so it reads over any hull.</summary>
    public static MeshRenderer BuildCockpit(Transform parent, Color color)
    {
        var vertices = new Vector3[13];
        var colors = new Color[13];
        var triangles = new int[6 * 9];
        for (int i = 0; i < 6; i++)
        {
            float angle = (i * 60 + 30) * Mathf.Deg2Rad;
            var corner = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle));
            vertices[i] = corner * CockpitRadius;
            colors[i] = color * .35f;
            vertices[6 + i] = corner * CockpitRadius * .72f + Vector3.back * .012f;
            colors[6 + i] = color;
        }
        vertices[12] = Vector3.back * .03f;
        colors[12] = Color.Lerp(color, Color.white, .65f);
        for (int i = 0; i < 6; i++)
        {
            int next = (i + 1) % 6, t = i * 9;
            triangles[t] = i;
            triangles[t + 1] = next;
            triangles[t + 2] = 6 + next;
            triangles[t + 3] = i;
            triangles[t + 4] = 6 + next;
            triangles[t + 5] = 6 + i;
            triangles[t + 6] = 6 + i;
            triangles[t + 7] = 6 + next;
            triangles[t + 8] = 12;
        }
        for (int i = 0; i < 13; i++)
            colors[i].a = 1;
        var mesh = new Mesh { name = "Hexagonal cockpit", vertices = vertices, colors = colors, triangles = triangles };
        var go = new GameObject("Cockpit");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = CockpitCentre;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = new Material(Resources.Load<Shader>("LevelScenery")) { name = "Shining cockpit" };
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        go.AddComponent<CockpitShine>();
        return renderer;
    }

    static void DestroyCockpit(MeshRenderer renderer)
    {
        Destroy(renderer.GetComponent<MeshFilter>().sharedMesh);
        Destroy(renderer.sharedMaterial);
        Destroy(renderer.gameObject);
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
        if (cockpit != null)
        {
            cockpit.sortingLayerID = modelRenderer.sortingLayerID;
            cockpit.sortingOrder = modelRenderer.sortingOrder + 1;
            cockpit.enabled = modelRenderer.enabled;
        }
        // The hitbox stays fully visible through invulnerability blinking; it only hides on death.
        if (hitboxRenderers != null)
            foreach (var marker in hitboxRenderers)
                if (marker != null)
                    marker.enabled = sourceSprite.color.a > 0.001f && !GameRules.playerDisabled;
    }
}

/// <summary>Makes a cockpit shine with a slow glint, through the scenery shader's flash.</summary>
public sealed class CockpitShine : MonoBehaviour
{
    static readonly int Flash = Shader.PropertyToID("_Flash");
    Material material;
    float phase;

    void Start()
    {
        material = GetComponent<MeshRenderer>().sharedMaterial;
        phase = Random.value * 6;
    }

    void Update()
    {
        float glint = Mathf.Pow(.5f + .5f * Mathf.Sin(Time.time * 2.6f + phase), 3);
        material.SetFloat(Flash, .08f + .3f * glint);
    }
}
