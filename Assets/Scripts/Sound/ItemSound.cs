using UnityEngine;
using System.Collections.Generic;

// Separate, bounded voices for item cues. Existing Sound Lab choices are untouched.
public sealed class ItemSound : MonoBehaviour
{
    static ItemSound instance;
    static readonly Dictionary<string, AudioClip> library = new Dictionary<string, AudioClip>();
    static readonly Dictionary<string, float> levels = new Dictionary<string, float>();
    static bool levelsLoaded;
    [System.Serializable]
    sealed class ApprovedLevels
    {
        public Level[] cues = null;
    }

    [System.Serializable]
    sealed class Level
    {
        public string cue = null; public float gain = 1;
    }

    readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    readonly Dictionary<string, float> gates = new Dictionary<string, float>();
    readonly AudioSource[] voices = new AudioSource[8];
    int cursor;
    bool paused;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetLibrary()
    {
        library.Clear();
        levels.Clear();
        levelsLoaded = false;
        instance = null;
    }

    public static float Gain(string cue)
    {
        if (!levelsLoaded)
        {
            levelsLoaded = true;
            var resource = Resources.Load<TextAsset>("ItemSoundGains");
            var approved = resource != null ? JsonUtility.FromJson<ApprovedLevels>(resource.text) : null;
            if (approved?.cues != null)
                foreach (var entry in approved.cues)
                    if (entry != null && !string.IsNullOrEmpty(entry.cue))
                        levels[entry.cue] = Mathf.Clamp(entry.gain, 0, 1.5f);
        }
        return levels.TryGetValue(cue, out float gain) ? gain : 1;
    }

    public static AudioClip Clip(string cue)
    {
        if (!library.TryGetValue(cue, out var clip))
            library[cue] = clip = Resources.Load<AudioClip>("Sounds/ItemApproved/" + cue) ?? Resources.Load<AudioClip>("Sounds/Items/" + cue);
        return clip;
    }

    // New candidates are auditioned first. Missing approvals preserve the previous cue.
    public static void Cue(string cue, string fallback = null, float volume = .14f, float pitch = 1, float cooldown = .12f)
    {
        string selected = Clip(cue) != null ? cue : fallback;
        if (!string.IsNullOrEmpty(selected))
            Play(selected, volume, pitch, cooldown);
    }

    public static void Play(string cue, float volume = .18f, float pitch = 1, float cooldown = .08f)
    {
        if (AudioListener.pause || Menu.GameplayPaused)
            return;
        if (instance == null)
            instance = new GameObject("Item sound voices").AddComponent<ItemSound>();
        instance.Emit(cue, volume, pitch, cooldown);
    }

    void Awake()
    {
        var manager = FindAnyObjectByType<SoundManager>();
        var routed = manager != null ? manager.GetComponent<AudioSource>() : null;
        for (int i = 0; i < voices.Length; i++)
        {
            var v = gameObject.AddComponent<AudioSource>();
            v.playOnAwake = false;
            v.spatialBlend = 0;
            v.priority = 110;
            if (routed != null)
                v.outputAudioMixerGroup = routed.outputAudioMixerGroup;
            voices[i] = v;
        }
    }

    void Emit(string cue, float volume, float pitch, float cooldown)
    {
        if (gates.TryGetValue(cue, out float next) && Time.unscaledTime < next)
            return;
        if (!clips.TryGetValue(cue, out var clip))
            clips[cue] = clip = Clip(cue);
        if (clip == null)
            return;
        gates[cue] = Time.unscaledTime + cooldown;
        var voice = voices[cursor++ % voices.Length];
        voice.Stop();
        voice.clip = clip;
        voice.pitch = pitch;
        voice.volume = volume * Gain(cue);
        voice.Play();
    }

    void Update()
    {
        if (paused == Menu.GameplayPaused)
            return;
        paused = Menu.GameplayPaused;
        foreach (var voice in voices)
            if (paused)
                voice.Pause();
            else
                voice.UnPause();
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
