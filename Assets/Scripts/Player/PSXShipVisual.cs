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
    public GameObject BuildDisplayHull(string terraModel, Color cockpitColor, int cockpitSides = 12)
    {
        CaptureDefault();
        var root = new GameObject("Parked " + (terraModel ?? "GR-07"));
        root.transform.localScale = transform.lossyScale;
        BuildCockpit(root.transform, cockpitColor, false, cockpitSides);
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

    // Every ship wears a small, shining round cockpit exactly over its hitbox, so the point that can be hit is always clear.
    // On the flown ship it sits on the banking axis (z = 0), so banking and rolls never carry it off the hitbox,
    // and it is drawn above hostile rounds.
    static readonly Vector2 CockpitCentre = new Vector2(0, .08f);
    const float CockpitRadius = .045f;
    Transform cockpitFrame;
    MeshRenderer cockpit;

    public void UseCockpit(Color color, int sides = 12)
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
        cockpit = BuildCockpit(cockpitFrame, color, true, sides);
        cockpit.gameObject.layer = gameObject.layer;
        Apply();
    }

    /// <summary>
    /// A glossy round canopy: dark rim, coloured glass and a white glint. It uses the ships' own shader, so on the
    /// player it shares the hull's foreground depth; parked copies sit just above their hull instead.
    /// </summary>
    public static MeshRenderer BuildCockpit(Transform parent, Color color, bool player = false, int sides = 12)
    {
        var palette = new Texture2D(4, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "Cockpit glass" };
        var rim = color * .45f;
        var glass = Color.Lerp(color, Color.white, .15f);
        var glint = Color.Lerp(color, Color.white, .85f);
        rim.a = glass.a = glint.a = 1;
        palette.SetPixels(new[] { rim, glass, glint, glass });
        palette.Apply();
        // Rim ring, glass ring and a centre crown.
        var vertices = new Vector3[sides * 2 + 1];
        var uvs = new Vector2[vertices.Length];
        var triangles = new int[sides * 9];
        int crown = sides * 2;
        for (int i = 0; i < sides; i++)
        {
            // A hexagon stands on a point at the nose; rounder canopies start anywhere.
            float angle = i * Mathf.PI * 2 / sides + (sides == 6 ? Mathf.PI / 2 : 0);
            var corner = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle));
            vertices[i] = corner * CockpitRadius;
            uvs[i] = new Vector2(.125f, .5f);
            vertices[sides + i] = corner * CockpitRadius * .7f + Vector3.back * .01f;
            uvs[sides + i] = new Vector2(.375f, .5f);
        }
        vertices[crown] = new Vector3(-.25f, .25f) * CockpitRadius + Vector3.back * .025f;
        uvs[crown] = new Vector2(.625f, .5f);
        for (int i = 0; i < sides; i++)
        {
            int next = (i + 1) % sides, t = i * 9;
            triangles[t] = i;
            triangles[t + 1] = sides + next;
            triangles[t + 2] = next;
            triangles[t + 3] = i;
            triangles[t + 4] = sides + i;
            triangles[t + 5] = sides + next;
            triangles[t + 6] = sides + i;
            triangles[t + 7] = crown;
            triangles[t + 8] = sides + next;
        }
        var mesh = new Mesh { name = sides == 6 ? "Hexagonal cockpit" : "Round cockpit", vertices = vertices, uv = uvs, triangles = triangles };
        mesh.RecalculateNormals();
        var go = new GameObject("Cockpit");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(CockpitCentre.x, CockpitCentre.y, player ? 0 : -.12f);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        var material = new Material(CombatMaterials.Fleet) { name = "Shining cockpit", mainTexture = palette };
        material.SetFloat("_PlayerDepth", player ? 1 : 0);
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        go.AddComponent<CockpitShine>();
        return renderer;
    }
    static void DestroyCockpit(MeshRenderer renderer)
    {
        Destroy(renderer.GetComponent<MeshFilter>().sharedMesh);
        Destroy(renderer.sharedMaterial.mainTexture);
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
            // Above hostile rounds, so the hitbox is never hidden.
            cockpit.sortingLayerName = "Bullet";
            cockpit.sortingOrder = 40;
            cockpit.enabled = modelRenderer.enabled;
        }
        // The hitbox stays fully visible through invulnerability blinking; it only hides on death.
        if (hitboxRenderers != null)
            foreach (var marker in hitboxRenderers)
                if (marker != null)
                    marker.enabled = sourceSprite.color.a > 0.001f && !GameRules.playerDisabled;
    }
}

/// <summary>Makes a cockpit shine with a slow glint, overdriving its tint past the hull's lighting.</summary>
public sealed class CockpitShine : MonoBehaviour
{
    static readonly int Tint = Shader.PropertyToID("_Tint");
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
        material.SetColor(Tint, Color.white * (1.7f + .8f * glint));
    }
}
