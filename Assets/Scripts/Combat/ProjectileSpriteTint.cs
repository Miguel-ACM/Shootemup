using UnityEngine;
using System.Collections.Generic;

// Rendering tint is independent from gameplay flashes/fades in SpriteRenderer.color.
public sealed class ProjectileSpriteTint : MonoBehaviour
{
    static readonly Dictionary<Sprite,Sprite> legacySprites=new Dictionary<Sprite,Sprite>();
    SpriteRenderer visual;
    MaterialPropertyBlock properties;
    float lastBurn=-1,lastPoison=-1;
    public Color Tint {get;private set;}
    public static void Apply(GameObject go,Color tint,bool friendly=false,bool neutralize=false)
    {
        var effect=go.GetComponent<ProjectileSpriteTint>() ?? go.AddComponent<ProjectileSpriteTint>();
        effect.Tint=tint;
        effect.visual=go.GetComponent<SpriteRenderer>();effect.visual.forceRenderingOff=false;
        effect.visual.sharedMaterial=CombatVisuals.ProjectileMaterial;
        effect.visual.sortingLayerName="Bullet";effect.visual.sortingOrder=friendly ? 16 : 18;
        effect.properties=new MaterialPropertyBlock();effect.properties.SetColor("_SpriteTint",tint);
        effect.properties.SetFloat("_Neutralize",neutralize ? 1 : 0);
        effect.lastBurn=effect.lastPoison=-1;effect.enabled=friendly;
        effect.SetCoatings(0,0);if(friendly)effect.LateUpdate();
    }
    public static void Friendly(GameObject go)
    {
        var round=go.GetComponent<PlayerRound>();
        Apply(go,round!=null && round.delivery==PlayerDelivery.Flame ? new Color(1,.45f,.12f) : new Color(.32f,.72f,.85f),true,true);
    }
    public static void LegacyEnemy(GameObject go)
    {
        var visual=go.GetComponent<SpriteRenderer>();if(visual==null)return;
        if(!(go.GetComponent<AEnemyBullet>() is ExpandingWave))
        {
            // Preserve legacy prefab scale/colliders while replacing its old bitmap.
            var original=visual.sprite;
            if(!legacySprites.TryGetValue(original,out var replacement))
            {
                var bounds=original.bounds.size;var source=ProjectileSprites.Load("standard");
                replacement=Sprite.Create(source.texture,source.rect,new Vector2(.5f,.5f),source.texture.width/Mathf.Max(bounds.x,bounds.y),0,SpriteMeshType.FullRect);
                legacySprites[original]=replacement;
            }
            visual.sprite=replacement;
        }
        Apply(go,HostileProjectileArt.Accent(typeof(SimpleRound)),false,true);
    }
    void LateUpdate()
    {
        var upgrades=CombatUpgrades.Current;
        SetCoatings(upgrades!=null && upgrades.Count(UpgradeKind.Gasoline)>0 ? 1 : 0,
            upgrades!=null && upgrades.Count(UpgradeKind.Poison)>0 ? 1 : 0);
    }
    void SetCoatings(float burn,float poison)
    {
        if(burn==lastBurn && poison==lastPoison)return;
        lastBurn=burn;lastPoison=poison;
        properties.SetFloat("_Burn",burn);properties.SetFloat("_Poison",poison);visual.SetPropertyBlock(properties);
    }
}
