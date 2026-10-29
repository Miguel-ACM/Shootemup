using UnityEngine;

// Hunger: every few ordinary combat waves the pact checks how many of their enemies died.
// Falling short costs a hit, and that hit can end the run.
public sealed class HungerPang : MonoBehaviour
{
    public const int Waves = 3;
    const float Appetite = 2 / 3f;
    static readonly Color Pang = new Color(.9f, .12f, .2f);
    int waves, spawned, killed;

    void OnEnable()
    {
        WaveOrchestrer.CombatWaveFinished += Fed;
    }

    void OnDisable()
    {
        WaveOrchestrer.CombatWaveFinished -= Fed;
    }

    void Fed(int waveEnemies, int waveKills)
    {
        spawned += waveEnemies;
        killed += waveKills;
        if (++waves < Waves)
            return;
        bool starving = spawned > 0 && killed < spawned * Appetite;
        waves = spawned = killed = 0;
        if (!starving)
            return;
        PSXFireballs.Shockwave(transform.position, 1.6f, .4f, Pang, 2, .5f);
        GetComponent<Shipcontroller>().Sacrifice(1, lethal: true);
    }
}
