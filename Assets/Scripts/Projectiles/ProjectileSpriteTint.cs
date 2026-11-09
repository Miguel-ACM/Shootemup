using UnityEngine;
using System.Collections.Generic;

// Rendering tint is independent from gameplay flashes/fades in SpriteRenderer.color.
public sealed class ProjectileSpriteTint : MonoBehaviour
{
    static readonly Dictionary<Sprite, Sprite> legacySprites = new Dictionary<Sprite, Sprite>();
    SpriteRenderer visual;
    MaterialPropertyBlock properties;
    float lastBurn = -1, lastPoison = -1;
    bool neutral;
    public Color Tint
    {
        get; private set;
    }

    // A hostile round's tone, which is also its Polarity Engine pole; -1 for player fire.
    public int Polarity
    {
        get; private set;
    } = -1;

    public static void Apply(GameObject go, Color tint, bool friendly = false, bool neutralize = false)
    {
        var effect = go.GetComponent<ProjectileSpriteTint>() ?? go.AddComponent<ProjectileSpriteTint>();
        effect.Tint = tint;
        effect.visual = go.GetComponent<SpriteRenderer>();
        effect.visual.forceRenderingOff = false;
        effect.neutral = neutralize;
        effect.visual.sharedMaterial = friendly ? CombatVisuals.ProjectileMaterial : LevelProjectilePalette.Material(neutralize);
        // Player fire passes under the ship, so hostile rounds over the hull stay readable.
        effect.visual.sortingLayerName = friendly ? "Player" : "Bullet";
        effect.visual.sortingOrder = friendly ? -5 : 18;
        if (effect.properties == null)
            effect.properties = new MaterialPropertyBlock();
        effect.properties.SetColor("_SpriteTint", tint);
        effect.properties.SetFloat("_Neutralize", neutralize ? 1 : 0);
        effect.properties.SetFloat("_Emission", 1.15f);
        effect.properties.SetColor("_PoisonTint", PlayerShotPalette.Poison);
        effect.lastBurn = effect.lastPoison = -1;
        effect.enabled = friendly;
        if (friendly)
        {
            effect.SetCoatings(0, 0);
            effect.LateUpdate();
        }
        else
            effect.SetHostile();
    }

    public void SetHostile() => SetTone(HostileTone.Bright);

    // Both tones share their district's materials, so dense patterns still batch.
    public void SetTone(HostileTone tone)
    {
        Polarity = (int)tone;
        Tint = LevelProjectilePalette.Accent(LevelRun.Current);
        var shared = LevelProjectilePalette.Material(neutral, tone);
        if (visual.sharedMaterial != shared)
            visual.sharedMaterial = shared;
        visual.SetPropertyBlock(null);
    }

    // A special hue (Plains' orange carriers) keeps the round's tone and its pole.
    public void SetAccent(Color color)
    {
        Tint = color;
        properties.SetColor("_SpriteTint", color);
        properties.SetFloat("_Burn", 0);
        properties.SetFloat("_Poison", 0);
        visual.SetPropertyBlock(properties);
    }

    public static void Friendly(GameObject go)
    {
        var round = go.GetComponent<PlayerRound>();
        Apply(go, round != null && round.delivery == PlayerDelivery.Flame ? new Color(1, .45f, .12f) : new Color(.32f, .72f, .85f), true, true);
    }

    public static void LegacyEnemy(GameObject go)
    {
        go.layer = ProjectileCollisionLayers.Hostile;
        var visual = go.GetComponent<SpriteRenderer>();
        if (visual == null)
            return;
        if (!(go.GetComponent<AEnemyBullet>() is ExpandingWave))
        {
            // Preserve legacy prefab scale/colliders while replacing its old bitmap.
            var original = visual.sprite;
            if (!legacySprites.TryGetValue(original, out var replacement))
            {
                var bounds = original.bounds.size;
                var source = ProjectileSprites.Load("standard");
                replacement = Sprite.Create(source.texture, source.rect, new Vector2(.5f, .5f), source.texture.width / Mathf.Max(bounds.x, bounds.y), 0, SpriteMeshType.FullRect);
                legacySprites[original] = replacement;
            }
            visual.sprite = replacement;
        }
        Apply(go, HostileProjectileArt.Accent(typeof(SimpleRound)), false, true);
    }

    void LateUpdate()
    {
        var upgrades = CombatUpgrades.Current;
        SetCoatings(upgrades != null && upgrades.Count(UpgradeKind.Gasoline) > 0 ? 1 : 0,
            upgrades != null && upgrades.Count(UpgradeKind.Poison) > 0 ? 1 : 0);
    }

    void SetCoatings(float burn, float poison)
    {
        if (burn == lastBurn && poison == lastPoison)
            return;
        lastBurn = burn;
        lastPoison = poison;
        properties.SetFloat("_Burn", burn);
        properties.SetFloat("_Poison", poison);
        visual.SetPropertyBlock(properties);
    }
}
