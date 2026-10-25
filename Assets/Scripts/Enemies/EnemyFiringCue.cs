using UnityEngine;

// One tiny muzzle sprite per ship, reused for both charge and release.
public sealed class EnemyFiringCue : MonoBehaviour
{
    SpriteRenderer light;
    Transform hull;
    float heat, flash;
    Vector3 flashPosition;
    public void Build(Transform model)
    {
        hull = model;
        var go = new GameObject("Charging muzzle");
        go.transform.SetParent(transform, false);
        light = go.AddComponent<SpriteRenderer>();
        light.sprite = CombatVisuals.Disc;
        light.sharedMaterial = CombatVisuals.ProjectileMaterial;
        light.sortingLayerName = "Bullet";
        light.sortingOrder = 20;
        light.enabled = false;
    }

    public void Warn(float seconds)
    {
        heat = seconds >= 0 && seconds < .32f ? 1 - seconds / .32f : 0;
    }

    public void Flash(Vector3 position)
    {
        flash = .085f;
        flashPosition = position;
    }

    void LateUpdate()
    {
        if (light == null)
            return;
        flash = Mathf.Max(0, flash - Time.deltaTime);
        bool show = !GameRules.playerDisabled && (heat > 0 || flash > 0);
        light.enabled = show;
        if (!show)
            return;
        light.transform.position = flash > 0 ? flashPosition + Vector3.back * .6f : hull.TransformPoint(new Vector3(0, -.75f, -.6f));
        float size = flash > 0 ? .19f * (flash / .085f) : Mathf.Lerp(.07f, .23f, heat);
        light.transform.localScale = Vector3.one * size;
        var color = Color.Lerp(LevelProjectilePalette.Accent(LevelRun.Current), Color.white, .35f);
        color.a = flash > 0 ? .9f : heat * .8f;
        light.color = color;
    }
}
