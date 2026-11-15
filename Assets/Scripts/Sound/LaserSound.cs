using UnityEngine;

// Beam tails need sustained audio, not a repeated gunshot every damage tick.
public sealed class LaserSound : MonoBehaviour
{
    static AudioClip chargeClip, ignitionClip, sustainClip, releaseClip;
    // The player's own beams (Beam Emitter, laser drones) have their own ignition and hum.
    static AudioClip playerIgnitionClip, playerSustainClip;
    static float lastCharge = -10, lastIgnition = -10, lastRelease = -10;
    static int sustaining;
    // The held hum sits lower than its source clip; at its original pitch it grated over long beams.
    const float SustainPitch = .7f;
    AudioSource transient, bed;
    bool ignited, cooling, counted, friendly, celestial;
    static AudioClip heavenCharge, heavenIgnition;
    public bool Sustaining => bed != null && bed.isPlaying;
    public static int SustainingCount => sustaining;
    string ChargeCue => celestial ? "heavenBeamCharge" : friendly ? "playerBeamCharge" : "beamCharge";
    string FireCue => celestial ? "heavenBeamFire" : friendly ? "shotBeam" : "beamFire";
    string SustainCue => celestial ? "heavenBeamSustain" : friendly ? "playerBeamSustain" : "beamSustain";
    void Prepare()
    {
        if (chargeClip == null)
        {
            chargeClip = Resources.Load<AudioClip>("Sounds/Beam/charge");
            ignitionClip = Resources.Load<AudioClip>("Sounds/Beam/ignite");
            sustainClip = Resources.Load<AudioClip>("Sounds/Beam/sustain");
            releaseClip = Resources.Load<AudioClip>("Sounds/Beam/release");
            playerIgnitionClip = Resources.Load<AudioClip>("Sounds/Beam/player_ignite");
            playerSustainClip = Resources.Load<AudioClip>("Sounds/Beam/player_sustain");
            heavenCharge = Resources.Load<AudioClip>("Sounds/Heaven/charge");
            heavenIgnition = Resources.Load<AudioClip>("Sounds/Heaven/ignite");
        }
        if (transient != null)
            return;
        var manager = FindAnyObjectByType<SoundManager>();
        var routed = manager != null ? manager.GetComponent<AudioSource>() : null;
        transient = gameObject.AddComponent<AudioSource>();
        bed = gameObject.AddComponent<AudioSource>();
        foreach (var source in new[] { transient, bed })
        {
            source.playOnAwake = false;
            source.spatialBlend = 0;
            source.priority = 100;
            if (routed != null)
                source.outputAudioMixerGroup = routed.outputAudioMixerGroup;
        }
        bed.loop = true;
        bed.clip = sustainClip;
    }

    public void Charge(float seconds, bool player = false, bool siege = false)
    {
        Prepare();
        Stop();
        friendly = player;
        celestial = siege;
        ignited = cooling = false;
        if (Time.time - lastCharge < .11f || AudioListener.pause)
            return;
        lastCharge = Time.time;
        transient.clip = SfxMix.Resolve(ChargeCue, celestial && heavenCharge != null ? heavenCharge : chargeClip);
        transient.pitch = Mathf.Clamp((celestial && heavenCharge != null ? heavenCharge.length : .8f) / Mathf.Max(.1f, seconds), .5f, 2f);
        // Enemy laser charge and ignition sit about half as loud as before (Miguel, 15 November 2026).
        transient.volume = (player ? .09f : .10f) * SfxMix.Gain(ChargeCue);
        transient.Play();
    }

    public void Drive(float power, bool firing)
    {
        if (transient == null)
            return;
        if (firing && !ignited)
        {
            ignited = true;
            transient.Stop();
            if (Time.time - lastIgnition > .075f)
            {
                lastIgnition = Time.time;
                transient.pitch = celestial ? .78f : 1;
                transient.volume = (friendly ? .10f : .13f) * SfxMix.Gain(FireCue);
                var ignition = SfxMix.Resolve(FireCue, celestial && heavenIgnition != null ? heavenIgnition : friendly && playerIgnitionClip != null ? playerIgnitionClip : ignitionClip);
                if (ignition != null) transient.PlayOneShot(ignition);
            }
            bed.clip = SfxMix.Resolve(SustainCue, friendly && playerSustainClip != null ? playerSustainClip : sustainClip);
            if (bed.clip != null)
            {
                bed.pitch = celestial ? .55f : SustainPitch;
                bed.time = (((GetEntityId().GetHashCode() & 0x7fffffff) % 7) * .071f) % bed.clip.length;
                bed.Play();
                counted = true;
                sustaining++;
            }
        }
        if (!firing && ignited && !cooling)
        {
            cooling = true;
            if (Time.time - lastRelease > .075f)
            {
                lastRelease = Time.time;
                transient.pitch = 1;
                transient.volume = (friendly ? .08f : .14f) * SfxMix.Gain("beamRelease");
                var release = SfxMix.Resolve("beamRelease", releaseClip);
                if (release != null) transient.PlayOneShot(release);
            }
        }
        // The emitter can cool faster on later loops. Fade the release with that
        // same envelope so pooling never cuts an audible sample in mid-waveform.
        if (cooling)
            transient.volume = (friendly ? .08f : .14f) * SfxMix.Gain("beamRelease") * Mathf.Clamp01(power / .4f);
        bed.volume = (friendly ? .065f : .15f) * SfxMix.Gain(SustainCue) * Mathf.Clamp01(power) / Mathf.Sqrt(Mathf.Max(1, sustaining));
        if (cooling && power <= .005f)
            StopBed();
    }

    void StopBed()
    {
        if (bed != null)
            bed.Stop();
        if (counted)
        {
            counted = false;
            sustaining = Mathf.Max(0, sustaining - 1);
        }
    }

    public void Stop()
    {
        StopBed();
        if (transient != null)
            transient.Stop();
    }

    void OnDisable()
    {
        Stop();
    }

    void OnDestroy()
    {
        Stop();
    }
}
