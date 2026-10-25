using UnityEngine;
using System.Collections.Generic;

// Separate, bounded voices for item cues. Existing Sound Lab choices are untouched.
public sealed class ItemSound : MonoBehaviour
{
    static ItemSound instance;
    readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    readonly Dictionary<string, float> gates = new Dictionary<string, float>();
    readonly AudioSource[] voices = new AudioSource[8];
    int cursor;
    bool paused;
    public static AudioClip Clip(string cue) => Resources.Load<AudioClip>("Sounds/Items/" + cue);
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
        voice.volume = volume;
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
