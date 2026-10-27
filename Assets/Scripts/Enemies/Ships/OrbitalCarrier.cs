using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class OrbitalCarrier : SpaceSatellite
{
    protected override void Configure()
    {
        hull = 43;
        hullScale = .64f;
        health = 135;
        attackInterval = (1.7f);
        Mover<TopTracker>();
    }

    protected override void Start()
    {
        base.Start();
        for (int i = 0; i < 2; i++)
        {
            var moon = Spawn(typeof(OrbitalMoonlet), transform.position + Vector3.right * (i == 0 ? 1.18f : -1.18f));
            ShareEncounterWith(moon);
            if (IsMiniboss)
                moon.FlyPhrase(new WaveUnit(nameof(OrbitalMoonlet), 0, Vector3.zero) { miniboss = true });
            var orbit = moon.gameObject.AddComponent<SatelliteOrbit>();
            orbit.Anchor = transform;
            orbit.Phase = i * Mathf.PI;
            moon.UseMovement(orbit);
        }
    }

    protected override void Attack()
    {
        Fan<RicochetBolt>((7), 22, 3.6f, .21f);
    }
}
