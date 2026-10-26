using UnityEngine;

// A small attached nozzle and one continuous voice; damage still belongs to existing flame rounds.
public sealed class FlameWeaponFeedback : MonoBehaviour
{
    AWeapon weapon;
    PSXShipEffects hull;
    Vector2 direction;
    readonly LineRenderer[] tongues = new LineRenderer[2];
    AudioSource bed;
    float heat;
    bool lit, paused;
    public bool HasApprovedLoop => bed != null && bed.clip != null;

    public void Build(AWeapon source, Vector2 fireDirection)
    {
        weapon = source;
        hull = source.GetComponentInParent<PSXShipEffects>();
        direction = fireDirection;
        bed = gameObject.AddComponent<AudioSource>();
        bed.playOnAwake = false;
        bed.loop = true;
        bed.spatialBlend = 0;
        bed.priority = 105;
        bed.clip = ItemSound.Clip("itemFlameSustain");
        var manager = FindAnyObjectByType<SoundManager>();
        if (manager != null) bed.outputAudioMixerGroup = manager.GetComponent<AudioSource>().outputAudioMixerGroup;
        for (int i = 0; i < tongues.Length; i++)
        {
            tongues[i] = CombatVisuals.Line(transform, PlayerShotPalette.Fire, .035f);
            tongues[i].positionCount = 13;
            tongues[i].sortingOrder = 13;
            tongues[i].enabled = false;
        }
    }

    public void Fire()
    {
        if (!lit)
        {
            lit = true;
            ItemSound.Cue("itemFlameIgnite", volume: .1f, cooldown: .1f);
            if (HasApprovedLoop && !bed.isPlaying) bed.Play();
        }
    }

    void LateUpdate()
    {
        if (weapon == null || bed == null) return;
        if (Menu.GameplayPaused)
        {
            if (!paused) { paused = true; bed.Pause(); }
            return;
        }
        if (paused) { paused = false; bed.UnPause(); }
        bool held = weapon.isShootingPrimary && !weapon.isStopingPrimary || weapon.isShootingSecondary && !weapon.isStopingSecondary;
        bool firing = lit && held && !GameRules.playerDisabled;
        if (lit && !firing)
        {
            lit = false;
            ItemSound.Cue("itemFlameRelease", volume: .08f, cooldown: .12f);
        }
        heat = Mathf.MoveTowards(heat, firing ? 1 : 0, WorldTimeStop.PlayerDelta / (firing ? .06f : .14f));
        bed.volume = heat * .07f * ItemSound.Gain("itemFlameSustain");
        if (heat <= .001f && bed.isPlaying) bed.Stop();
        Vector3 muzzle = transform.position + (Vector3)direction * .3f;
        if (hull != null)
            muzzle = direction.y > 0
                ? (hull.MuzzlePosition(0) + hull.MuzzlePosition(1)) * .5f
                : hull.transform.TransformPoint(new Vector3(0, hull.exhaustOffset.y, -.03f) * hull.HullScale);
        Vector2 right = new Vector2(direction.y, -direction.x);
        for (int line = 0; line < 2; line++)
        {
            var tongue = tongues[line];
            tongue.enabled = heat > .01f;
            if (!tongue.enabled) continue;
            Color color = PlayerShotPalette.Fire;
            color.a = heat * .55f;
            tongue.startColor = tongue.endColor = color;
            tongue.startWidth = .045f * heat;
            tongue.endWidth = .018f * heat;
            int sign = line == 0 ? -1 : 1;
            for (int i = 0; i < 13; i++)
            {
                float t = i / 12f;
                Vector2 offset = direction * (.38f * t) + right * (sign * .10f * Mathf.Sin(t * Mathf.PI) + .025f * Mathf.Sin(Time.unscaledTime * 29 + t * 7) * t);
                Vector3 p = muzzle + (Vector3)(offset * heat);
                p.z = -1.1f;
                tongue.SetPosition(i, p);
            }
        }
    }

    void OnDisable()
    {
        lit = false;
        heat = 0;
        if (bed != null) bed.Stop();
        foreach (var tongue in tongues) if (tongue != null) tongue.enabled = false;
    }
}
