using UnityEngine;

public static class HeavenArmory
{
    // Existing pooled delivery, bloom and hit logic; no new projectile sprites or lights.
    public static PulseLaser Lance(Transform source, Vector3 local, Vector2 axis, float width, float hold, float charge = 1.25f, FiringTier? firingTier = null)
    {
        var beam=CombatRound.Launch<PulseLaser>(source.TransformPoint(local),axis,0,firingTier: firingTier);
        if(beam==null) return null;
        beam.Bind(source,local);beam.width=width;beam.widthRise=Mathf.Min(.65f,hold*.4f);beam.warning=beam.Firing.Rest(charge);
        beam.activeTime=hold;beam.celestial=true;
        return beam;
    }
}
