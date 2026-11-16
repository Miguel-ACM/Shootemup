using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SoundManager : MonoBehaviour
{
    public static AudioClip hit1, shot1, enemyExplosion1, hurt1, playerDead, bossWarning;
    public static AudioClip blankbullet;
    static AudioSource audioSource, scheduledSource;
    // Pitch is per AudioSource and also bends one-shots that are still playing, so pitched sounds
    // get their own sources instead of retuning the shared one under every other sound.
    static AudioSource[] pitchedSources;
    static int nextPitchedSource = 0;
    static AudioSource[] enemySources;
    static int nextEnemySource;
    static float nextEnemyEvent;
    static float nextEnemyImpact;
    bool paused;

    // Procedural bank from Tools/synth_sfx.py: Sounds/Synth/<cue>_NN variants. F9 flips between it and
    // the previous sounds for in-game A/B; cues that only exist in the synth bank go silent when it is off.
    static readonly Dictionary<string, AudioClip[]> synth = new Dictionary<string, AudioClip[]>();
    static readonly Dictionary<string, int> lastVariant = new Dictionary<string, int>();
    static readonly Dictionary<string, float> gates = new Dictionary<string, float>();
    public static bool SynthEnabled { get; private set; } = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetBank()
    {
        synth.Clear();
        lastVariant.Clear();
        gates.Clear();
        audioSource = scheduledSource = null;
        pitchedSources = enemySources = null;
        nextPitchedSource = nextEnemySource = 0;
        nextEnemyEvent = nextEnemyImpact = 0;
    }

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        pitchedSources = new AudioSource[4];
        for (int i = 0; i < pitchedSources.Length; i++)
        {
            AudioSource s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
            s.spatialBlend = audioSource.spatialBlend;
            pitchedSources[i] = s;
        }
        hit1 = Resources.Load<AudioClip>("Sounds/hit1");
        shot1 = Resources.Load<AudioClip>("Sounds/shot1");
        enemyExplosion1 = Resources.Load<AudioClip>("Sounds/enemyExplosion1");
        hurt1 = Resources.Load<AudioClip>("Sounds/hurt1");
        playerDead = Resources.Load<AudioClip>("Sounds/playerDead");
        blankbullet = Resources.Load<AudioClip>("Sounds/blankbullet");
        bossWarning = Resources.Load<AudioClip>("Sounds/bossWarning");
        SynthEnabled = PlayerPrefs.GetInt("SynthSfx", 1) == 1;
        LoadSynth();
        SoundCueCatalog.Warm();
        enemySources = new AudioSource[6];
        for (int i = 0; i < enemySources.Length; i++)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0;
            source.priority = 180;
            source.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
            enemySources[i] = source;
        }
        foreach (var cue in SoundCueCatalog.All)
            gates[cue.Id] = 0;
    }

    static void LoadSynth()
    {
        if (synth.Count > 0)
            return;
        var groups = new Dictionary<string, List<AudioClip>>();
        foreach (var clip in Resources.LoadAll<AudioClip>("Sounds/Synth"))
        {
            int cut = clip.name.LastIndexOf('_');
            string cue = cut > 0 ? clip.name.Substring(0, cut) : clip.name;
            if (!groups.TryGetValue(cue, out var list))
                groups[cue] = list = new List<AudioClip>();
            list.Add(clip);
        }
        foreach (var pair in groups)
        {
            pair.Value.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            synth[pair.Key] = pair.Value.ToArray();
        }
    }

    void Update()
    {
        if (paused != Menu.GameplayPaused)
        {
            paused = Menu.GameplayPaused;
            foreach (var source in enemySources)
                if (paused) source.Pause(); else source.UnPause();
        }
        if (Keyboard.current == null || !Keyboard.current.f9Key.wasPressedThisFrame)
            return;
        SynthEnabled = !SynthEnabled;
        PlayerPrefs.SetInt("SynthSfx", SynthEnabled ? 1 : 0);
        PlaySound("shot1", .1f);
    }

    // A random variant that never repeats the previous one, so rapid cues don't machine-gun.
    public static AudioClip Synth(string cue)
    {
        if (!SynthEnabled)
            return null;
        LoadSynth();
        if (!synth.TryGetValue(cue, out var variants) || variants.Length == 0)
            return null;
        lastVariant.TryGetValue(cue, out int last);
        int pick = variants.Length == 1 ? 0 : (last + Random.Range(1, variants.Length)) % variants.Length;
        lastVariant[cue] = pick;
        return variants[pick];
    }

    public static bool HasSynth(string cue)
    {
        if (SfxMix.Override(cue) != null)
            return true;
        LoadSynth();
        return SynthEnabled && synth.ContainsKey(cue);
    }

    static AudioClip GetClip(string clip)
    {
        var selected = SfxMix.Override(clip);
        if (selected != null)
            return selected;
        var generated = Synth(clip);
        if (generated != null)
            return generated;
        switch (clip)
        {
            case "hit1":
                return hit1;
            case "shot1":
                return shot1;
            case "enemyExplosion1":
                return enemyExplosion1;
            case "hurt1":
                return hurt1;
            case "playerDead":
                return playerDead;
            case "blankbullet":
                return blankbullet;
            case "bossWarning":
                return bossWarning;
        }
        // Splitting the weapon controls must preserve the previous shot bank until a take is chosen.
        var definition = SoundCueCatalog.Find(clip);
        if (definition != null && definition.Group == "Player weapons" && definition.Resource == "Sounds/shot1")
            return Synth("shot1") ?? shot1;
        return SoundCueCatalog.DefaultClip(clip);
    }

    public static void PlaySoundPitch(string clip, float volume, float pitch)
    {
        if (audioSource == null)
            return;
        volume *= SfxMix.Gain(clip);
        if (volume <= 0)
            return;
        AudioClip c = GetClip(clip);
        if (c == null)
            return;
        AudioSource source = audioSource;
        if (pitch != 1f)
        {
            source = pitchedSources[nextPitchedSource];
            nextPitchedSource = (nextPitchedSource + 1) % pitchedSources.Length;
        }
        source.pitch = pitch;
        source.PlayOneShot(c, volume);
    }

    public static void PlayEnemyLaunch(System.Type type, Vector3 position)
    {
        if (position.y > Camera.UpLimit + 1 || position.y < Camera.DownLimit - 1)
            return;
        PlayEnemyEvent(SoundCueCatalog.Projectile(type));
    }

    // One sound per volley accent, never one voice per round. No random draws affect seeded combat.
    public static void PlayEnemyEvent(string cue)
    {
        if (cue == null || enemySources == null || AudioListener.pause || Menu.GameplayPaused || Time.timeScale == 0)
            return;
        float now = Time.unscaledTime;
        bool launch = cue.StartsWith("enemy", System.StringComparison.Ordinal);
        if (now < (launch ? nextEnemyEvent : nextEnemyImpact) || (gates.TryGetValue(cue, out var next) && now < next))
            return;
        var definition = SoundCueCatalog.Find(cue);
        if (definition == null)
            return;
        float volume = definition.Volume * SfxMix.Gain(cue);
        if (volume <= 0)
            return;
        var clip = SfxMix.Resolve(cue, SoundCueCatalog.DefaultClip(cue));
        if (clip == null)
            return;
        gates[cue] = now + definition.Rest;
        if (launch) nextEnemyEvent = now + .045f; else nextEnemyImpact = now + .08f;
        var source = enemySources[nextEnemySource++ % enemySources.Length];
        source.Stop();
        source.clip = clip;
        source.pitch = definition.Pitch;
        source.volume = volume;
        source.Play();
    }

    public static void PlaySoundRandomPitch(string clip, float volume, float[] pitchRange)
    {
        PlaySoundPitch(clip, volume, Random.Range(pitchRange[0], pitchRange[1]));
    }

    public static void PlaySound(string clip, float volume)
    {
        PlaySoundPitch(clip, volume, 1f);
    }

    public static bool TryScheduleSound(string cue, float volume, double at)
    {
        if (audioSource == null)
            return false;
        volume *= SfxMix.Gain(cue);
        if (volume <= 0)
            return true;
        var clip = GetClip(cue);
        if (clip == null)
            return false;

        if (scheduledSource == null)
        {
            scheduledSource = audioSource.gameObject.AddComponent<AudioSource>();
            scheduledSource.playOnAwake = false;
            scheduledSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
            scheduledSource.spatialBlend = audioSource.spatialBlend;
        }
        scheduledSource.Stop();
        scheduledSource.clip = clip;
        scheduledSource.volume = volume;
        scheduledSource.PlayScheduled(at);
        return true;
    }

    // Frequent event cues (hits, pickups) share a per-cue gate so dense combat doesn't stack them.
    public static void PlayGated(string clip, float volume, float cooldown)
    {
        if (gates.TryGetValue(clip, out float next) && Time.unscaledTime < next)
            return;
        gates[clip] = Time.unscaledTime + cooldown;
        PlaySound(clip, volume);
    }
}
