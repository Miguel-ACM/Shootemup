using System.Collections;
using UnityEngine;

// Warm the bounded set of formation geometry in short slices behind each district curtain.
public sealed class WavePreparation : MonoBehaviour
{
    static WavePreparation current;
    static ShaderVariantCollection projectileShaders;
    Coroutine job;
    public static bool Ready => current == null || current.job == null;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        current = null;
        projectileShaders = null;
    }

    public static void Begin()
    {
        if (!Application.isPlaying)
            return;
        if (current == null)
            current = new GameObject("Wave geometry preparation").AddComponent<WavePreparation>();
        if (current.job != null)
            current.StopCoroutine(current.job);
        current.job = current.StartCoroutine(current.Prepare());
    }

    IEnumerator Prepare()
    {
        // Compile/upload the projectile pass behind the curtain, before the first volley.
        if (projectileShaders == null)
        {
            projectileShaders = new ShaderVariantCollection();
            projectileShaders.Add(new ShaderVariantCollection.ShaderVariant(CombatVisuals.ProjectileMaterial.shader, UnityEngine.Rendering.PassType.ScriptableRenderPipeline));
            projectileShaders.WarmUp();
        }
        foreach (PlayerDelivery delivery in System.Enum.GetValues(typeof(PlayerDelivery)))
            PlayerProjectileArt.For(delivery);
        if (LevelRun.Current == LevelId.Space)
            _ = SpaceMaterials.Fleet;
        PSXImpactEffects.Warm();
        yield return PSXFireballs.Warm();
        yield return null;
        yield return CombatRound.PrepareBursts();
        yield return WaveComposer.Prepare();
        // Complete the seeded plan before the opening breather, keeping combat cache-only.
        for (int budget = 2; budget <= 20; budget++)
        {
            WaveComposer.Create(budget);
            yield return null;
        }
        job = null;
    }
}
