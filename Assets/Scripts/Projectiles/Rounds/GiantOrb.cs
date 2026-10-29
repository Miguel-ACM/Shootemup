using UnityEngine;

// A huge slow orb: a bright rim around a dark spiked core. It uses every authored
// behaviour of an ordinary round and slowly turns so the spikes read as alive.
public sealed class GiantOrb : CombatRound
{
    protected override void Tick(float dt)
    {
        Advance(dt);
        transform.rotation = Quaternion.Euler(0, 0, age * 40);
    }
}
