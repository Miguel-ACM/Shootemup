using System;
using System.Collections.Generic;
using UnityEngine;

// Shared by gameplay and the editor audition window. An empty entry preserves the authored sound.
public sealed class SfxMix : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public string cue;
        public AudioClip clip;
        [Range(0, 2)] public float gain = 1;
        public bool muted;
        public string choice;
    }

    public List<Entry> entries = new List<Entry>();
    Dictionary<string, Entry> index;
    static SfxMix current;
    static bool loaded;
    public static SfxMix Current
    {
        get
        {
            if (!loaded)
            {
                current = Resources.Load<SfxMix>("SfxMix");
                loaded = true;
            }
            return current;
        }
    }

    public Entry Find(string cue)
    {
        if (string.IsNullOrEmpty(cue))
            return null;
        if (index == null)
        {
            index = new Dictionary<string, Entry>(StringComparer.Ordinal);
            foreach (var entry in entries)
                if (entry != null && !string.IsNullOrEmpty(entry.cue))
                    index[entry.cue] = entry;
        }
        return index.TryGetValue(cue, out var found) ? found : null;
    }

    public Entry Edit(string cue)
    {
        var found = Find(cue);
        if (found != null)
            return found;
        found = new Entry { cue = cue };
        entries.Add(found);
        Invalidate();
        return found;
    }

    public void Invalidate() => index = null;
    void OnValidate() => Invalidate();

    public static float Gain(string cue)
    {
        var entry = Current != null ? Current.Find(cue) : null;
        if (entry == null)
            return 1;
        return entry.muted ? 0 : float.IsFinite(entry.gain) ? Mathf.Clamp(entry.gain, 0, 2) : 1;
    }

    public static AudioClip Override(string cue)
    {
        var clip = Current != null ? Current.Find(cue)?.clip : null;
        // Unity's empty serialized object references can be non-null CLR wrappers.
        return clip != null ? clip : null;
    }
    public static AudioClip Resolve(string cue, AudioClip fallback)
    {
        var clip = Override(cue);
        return clip != null ? clip : fallback;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        current = null;
        loaded = false;
    }
}
