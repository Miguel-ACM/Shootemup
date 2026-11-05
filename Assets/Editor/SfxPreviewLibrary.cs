using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

// Reads the existing browser library without changing its manifest, choices or backups.
public static class SfxPreviewLibrary
{
    public const string MixPath = "Assets/Resources/SfxMix.asset";
    public static string BankRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "../SoundLab/public"));
    [Serializable] public sealed class Take
    {
        public string id = null, label = null, description = null, file = null;
    }
    [Serializable] public sealed class Cue
    {
        public string id = null, label = null, group = null, mode = null;
        public float gameGain = .1f;
        public Take original = null;
        public Take[] choices = Array.Empty<Take>();
        [NonSerialized] public float pitch = 1;
        public bool Loop => mode == "loop" || id == "itemFlameSustain";
    }
    [Serializable] sealed class Manifest { public Cue[] cues = Array.Empty<Cue>(); }
    [DataContract] public sealed class SavedPicks
    {
        [DataMember] public Dictionary<string, Pick> picks = new Dictionary<string, Pick>();
    }
    [DataContract] public sealed class Pick
    {
        [DataMember] public string choice;
        [DataMember] public float gain = 1;
        [OnDeserializing] void Defaults(StreamingContext context) => gain = 1;
    }
    static readonly Dictionary<string, string> Fallbacks = new Dictionary<string, string>
    {
        { "itemChargeRelease", "cannon" }, { "itemChargeBlast", "cannon" },
        { "itemCapReady", "ready" }, { "itemGum", "popcorn" }
    };
    public static string Canonical(string id) => id == "explosionHeavy" ? "explosionLarge" : id;

    public static SfxMix EnsureMix()
    {
        var mix = AssetDatabase.LoadAssetAtPath<SfxMix>(MixPath);
        if (mix != null) return mix;
        mix = ScriptableObject.CreateInstance<SfxMix>();
        AssetDatabase.CreateAsset(mix, MixPath);
        AssetDatabase.SaveAssetIfDirty(mix);
        return mix;
    }

    public static List<Cue> Load()
    {
        var result = new Dictionary<string, Cue>();
        string manifest = Path.Combine(BankRoot, "manifest.json");
        if (File.Exists(manifest))
            foreach (var cue in JsonUtility.FromJson<Manifest>(File.ReadAllText(manifest)).cues)
            {
                // JsonUtility can materialize an empty serializable object for JSON null.
                if (string.IsNullOrWhiteSpace(cue.original?.file)) cue.original = null;
                cue.id = Canonical(cue.id);
                result[cue.id] = cue;
            }
        foreach (var definition in SoundCueCatalog.All)
        {
            if (!result.TryGetValue(definition.Id, out var cue))
                result[definition.Id] = cue = new Cue { id = definition.Id };
            cue.label = definition.Label;
            cue.group = definition.Group;
            cue.gameGain = definition.Volume;
            cue.pitch = definition.Pitch;
            if (definition.Loop) cue.mode = "loop";
        }
        foreach (var directory in new[] { "Sounds", "Sounds/Synth", "Sounds/Items", "Sounds/ItemApproved" })
        {
            string folder = "Assets/Resources/" + directory;
            if (!Directory.Exists(folder)) continue;
            foreach (var file in Directory.GetFiles(folder, "*.wav", SearchOption.TopDirectoryOnly))
            {
                string id = Path.GetFileNameWithoutExtension(file);
                if (directory.EndsWith("Synth", StringComparison.Ordinal))
                {
                    int underscore = id.LastIndexOf('_');
                    if (underscore > 0) id = id.Substring(0, underscore);
                }
                if (id == "BossKick" || result.ContainsKey(id)) continue;
                result[id] = new Cue { id = id, label = ObjectNames.NicifyVariableName(id),
                    group = directory.Contains("Items") || directory.Contains("Approved") ? "Item effects" : "Hits, rewards & UI",
                    gameGain = directory.Contains("Items") ? .18f : .1f };
            }
        }
        // Representative authored levels; gameplay still owns contextual pitch and attenuation.
        var levels = new Dictionary<string, float> {
            {"playerDead", .5f}, {"itemFlameSustain", .07f}, {"itemFlameIgnite", .1f},
            {"itemFlameRelease", .08f}, {"itemDepthLaunch", .1f}, {"itemChargeRelease", .17f},
            {"itemChargeBlast", .14f}, {"itemDepthBurst", .11f}, {"itemProximity", .11f},
            {"itemBearing", .07f}, {"itemReturn", .07f}, {"itemRicochet", .07f},
            {"itemBurn", .06f}, {"itemPoison", .065f}, {"itemFocus", .065f},
            {"itemChain", .13f}, {"itemReserve", .08f}, {"itemSplit", .08f}, {"itemGum", .08f}
        };
        foreach (var pair in levels)
            if (result.TryGetValue(pair.Key, out var cue)) cue.gameGain = pair.Value;
        return result.Values.OrderBy(c => c.group).ThenBy(c => c.label).ToList();
    }

    public static AudioClip DefaultClip(string id)
    {
        var approved = Resources.Load<AudioClip>("Sounds/ItemApproved/" + id);
        if (approved != null) return approved;
        // Preview a stable representative of the active runtime bank, without consuming gameplay RNG.
        var definition = SoundCueCatalog.Find(id);
        bool enemy = id.StartsWith("enemy", StringComparison.Ordinal) && definition != null;
        bool usesSamples = enemy || definition?.Group == "Lasers" || id == "shellBurst" || id == "ricochetHit" || id == "plasmaBreak";
        if (!usesSamples && PlayerPrefs.GetInt("SynthSfx", 1) == 1)
        {
            string synthId = definition?.Resource == "Sounds/shot1" ? "shot1" : id;
            var clips = Resources.LoadAll<AudioClip>("Sounds/Synth");
            var clip = clips.Where(c => c.name.StartsWith(synthId + "_", StringComparison.Ordinal)).OrderBy(c => c.name).FirstOrDefault();
            if (clip != null) return clip;
        }
        var fallback = SoundCueCatalog.DefaultClip(id) ?? Resources.Load<AudioClip>("Sounds/" + id) ?? Resources.Load<AudioClip>("Sounds/Items/" + id);
        if (fallback == null && Fallbacks.TryGetValue(id, out var older))
            fallback = SfxMix.Override(older) ?? DefaultClip(older);
        return fallback;
    }

    public static string SamplePath(Take take)
    {
        if (string.IsNullOrWhiteSpace(take?.file)) throw new IOException("This take has no audio file.");
        string path = Path.GetFullPath(Path.Combine(BankRoot, take.file));
        if (!path.StartsWith(BankRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Sound Lab file is outside its audio library.");
        return path;
    }

    public static AudioClip ImportTake(string cue, Take take)
    {
        byte[] bytes = File.ReadAllBytes(SamplePath(take));
        // Validate before adding the user's chosen take to Unity.
        SfxPreviewAudio.ReadWave(bytes);
        string hash;
        using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").Substring(0, 16).ToLowerInvariant();
        const string folder = "Assets/Resources/Sounds/Selected";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Resources/Sounds", "Selected");
        string safe = new string(cue.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
        string path = folder + "/" + safe + "_" + hash + ".wav";
        if (!File.Exists(path))
        {
            File.WriteAllBytes(path, bytes);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = false;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    public static SavedPicks ReadSavedPicks(string path)
    {
        using (var stream = File.OpenRead(path))
            return (SavedPicks)new DataContractJsonSerializer(typeof(SavedPicks),
                new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true }).ReadObject(stream);
    }

    public static int ImportSavedPicks(SfxMix mix, List<Cue> cues, string path)
    {
        var saved = ReadSavedPicks(path);
        int count = 0;
        Undo.RecordObject(mix, "Import Sound Lab picks");
        foreach (var pair in saved.picks)
        {
            var cue = cues.Find(c => c.id == Canonical(pair.Key));
            var pick = pair.Value;
            if (cue == null || pick == null) continue;
            // Pending and rejected candidates never become approvals. Keep current preserves Unity's sound.
            bool keep = pick.choice == "current" || pick.choice == "original";
            var take = cue.choices?.FirstOrDefault(t => t.id == pick.choice);
            if (take == null && !keep) continue;
            var clip = take != null ? ImportTake(cue.id, take) : null;
            var entry = mix.Edit(cue.id);
            if (!keep) { entry.clip = clip; entry.choice = "Sound Lab " + pick.choice; }
            entry.gain = float.IsFinite(pick.gain) ? Mathf.Clamp(pick.gain, 0, 2) : 1;
            count++;
        }
        if (count > 0) { EditorUtility.SetDirty(mix); AssetDatabase.SaveAssetIfDirty(mix); }
        return count;
    }
}
