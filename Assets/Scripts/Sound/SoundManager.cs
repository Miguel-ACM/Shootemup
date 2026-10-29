using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SoundManager : MonoBehaviour
{
    public static AudioClip hit1, shot1, enemyExplosion1, hurt1, playerDead, bossWarning;
    public static AudioClip blankbullet;
    static AudioSource audioSource;
    // Pitch is per AudioSource and also bends one-shots that are still playing, so pitched sounds
    // get their own sources instead of retuning the shared one under every other sound.
    static AudioSource[] pitchedSources;
    static int nextPitchedSource = 0;

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
        LoadSynth();
        return SynthEnabled && synth.ContainsKey(cue);
    }

    static AudioClip GetClip(string clip)
    {
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
        return null;
    }

    public static void PlaySoundPitch(string clip, float volume, float pitch)
    {
        if (audioSource == null)
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

    public static void PlaySoundRandomPitch(string clip, float volume, float[] pitchRange)
    {
        PlaySoundPitch(clip, volume, Random.Range(pitchRange[0], pitchRange[1]));
    }

    public static void PlaySound(string clip, float volume)
    {
        PlaySoundPitch(clip, volume, 1f);
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
