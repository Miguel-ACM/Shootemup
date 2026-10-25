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
            ? Quaternion.AngleAxis(bank + (GetComponent<ShipBarrelRoll>()?.Angle ?? 0), Vector3.up) * baseRotation
            // Face the imported nose toward +Y while keeping the dorsal side toward -Z.
            : Quaternion.Euler(-75f, 0f, 0f) * Quaternion.Euler(0f, 180f, 0f) * Quaternion.AngleAxis(bank, Vector3.forward);
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
        properties.SetColor(Tint, sourceSprite.color);
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
