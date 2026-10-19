using UnityEngine;

/// <summary>Connects a 3D visual to the existing 2D player's colour and movement.</summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class PSXShipVisual : MonoBehaviour
{
    [SerializeField] SpriteRenderer sourceSprite;
    [SerializeField] Transform model;
    [SerializeField] MeshRenderer modelRenderer;
    [SerializeField] float bankDegrees = 12f;
    MaterialPropertyBlock properties;
    Vector3 previousPosition;
    float bank;
    static readonly int Tint = Shader.PropertyToID("_Tint");

    public void Configure(SpriteRenderer sprite, Transform visual, MeshRenderer meshRenderer)
    {
        sourceSprite = sprite;
        model = visual;
        modelRenderer = meshRenderer;
        Apply();
    }

    void OnEnable()
    {
        previousPosition = transform.position;
        Apply();
    }

    void LateUpdate()
    {
        if (model == null || modelRenderer == null || sourceSprite == null) return;
        if (Application.isPlaying && Time.deltaTime > 0f)
        {
            float speed = (transform.position.x - previousPosition.x) / Time.deltaTime;
            float target = Mathf.Clamp(-speed / 8f, -1f, 1f) * bankDegrees;
            bank = Mathf.Lerp(bank, target, 1f - Mathf.Exp(-12f * Time.deltaTime));
        }
        previousPosition = transform.position;
        // Face the imported nose toward +Y while keeping the dorsal side toward -Z.
        model.localRotation = Quaternion.Euler(-75f, 0f, 0f)
            * Quaternion.Euler(0f, 180f, 0f) * Quaternion.AngleAxis(bank, Vector3.forward);
        Apply();
    }

    void Apply()
    {
        if (sourceSprite == null || modelRenderer == null) return;
        sourceSprite.enabled = false;
        modelRenderer.sortingLayerID = sourceSprite.sortingLayerID;
        modelRenderer.sortingOrder = sourceSprite.sortingOrder;
        if (properties == null) properties = new MaterialPropertyBlock();
        modelRenderer.GetPropertyBlock(properties);
        properties.SetColor(Tint, sourceSprite.color);
        modelRenderer.SetPropertyBlock(properties);
        modelRenderer.enabled = sourceSprite.color.a > 0.001f;
    }
}
