// Disabled with the rejected procedural sound pass. Kept for reference only.
#if false
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>Asset and voice scheduling regression checks, without changing scenes or saves.</summary>
[InitializeOnLoad]
public static class VerifyPSXSfx
{
    const string Request = "ArtSource/AudioReview/verify.request";
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static VerifyPSXSfx() { EditorApplication.update += Tick; }
    static void Tick()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        // A shell writer may still hold the request handle this frame; try again next update.
        try { File.Delete(Request); } catch (IOException) { return; }
        Run();
    }

    [MenuItem("Tools/Audio/Verify PSX SFX")]
    public static void Run()
    {
        var checks = new List<string>();
        GameObject test = null;
        try
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Run verification outside Play mode");
            int count = 0;
            foreach (var cue in SfxCatalog.All)
                for (int v = 1; v <= SfxCatalog.Variants; v++)
                {
                    string path = "Assets/Resources/Sounds/PSX/" + cue.Name + "_" + v.ToString("00") + ".wav";
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    Require(clip != null, "Missing imported sound: " + path);
                    Require(clip.channels == 1 && clip.frequency == 22050, "Wrong audio format: " + path);
                    var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                    Require(importer.defaultSampleSettings.compressionFormat == AudioCompressionFormat.PCM, "One-shots must use PCM: " + path);
                    Require(File.ReadAllText(path + ".meta").Contains("  normalize: 0") && importer.defaultSampleSettings.preloadAudioData, "Importer changes authored gain or streaming: " + path);
                    var samples = new float[clip.samples];
                    Require(clip.GetData(samples, 0), "Cannot decode sound: " + path);
                    float peak = 0;
                    foreach (float sample in samples) { Require(float.IsFinite(sample), "Non-finite PCM: " + path); peak = Mathf.Max(peak, Mathf.Abs(sample)); }
                    Require(peak > .1f && peak < .63f && samples[0] == 0 && samples[samples.Length - 1] == 0, "Clipping, silent clip or nonzero boundaries: " + path);
                    count++;
                }
            checks.Add("PASS: " + count + " imported mono PCM clips; authored levels and boundaries preserved.");
            // Edit-mode sources let us inspect the real scheduling code without entering gameplay.
            var existing = typeof(SoundManager).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Require(existing == null || (UnityEngine.Object)existing == null, "An active SoundManager already exists; stop Play mode first");
            var randomState = UnityEngine.Random.state;
            test = new GameObject("Temporary PSX audio verification") { hideFlags = HideFlags.HideAndDontSave };
            var manager = test.AddComponent<SoundManager>();
            typeof(SoundManager).GetMethod("Awake", Private).Invoke(manager, null);
            var sources = test.GetComponents<AudioSource>();
            Require(sources.Length == 24, "Voice pool must remain bounded at 24 sources");
            SoundManager.PlayCue("shot1");
            int before = Occupied(sources);
            var first = Array.Find(sources, s => s.clip != null);
            float pitch = first.pitch;
            var clipBefore = first.clip;
            for (int i = 0; i < 100; i++) SoundManager.PlayCue("shot1");
            Require(Occupied(sources) == before, "A simultaneous volley bypassed the cue gate");
            SoundManager.PlayCue("hit1"); SoundManager.PlayCue("enemyExplosion1");
            Require(first.pitch == pitch && first.clip == clipBefore, "A new sound retuned or replaced an active voice");
            checks.Add("PASS: simultaneous pellets coalesce; overlapping sounds retain independent pitches/clips.");
            var banks = (IDictionary)typeof(SoundManager).GetField("banks", Private).GetValue(manager);
            var voices = (Array)typeof(SoundManager).GetField("voices", Private).GetValue(manager);
            foreach (var cue in SfxCatalog.All)
            {
                AudioClip previous = null;
                for (int i = 0; i < 12; i++)
                {
                    ResetVoices(voices, banks);
                    SoundManager.PlayCue(cue.Name);
                    var selected = Array.Find(sources, s => s.clip != null);
                    Require(selected != null && selected.clip.name.StartsWith(cue.Name + "_", StringComparison.Ordinal), "Incorrect routing: " + cue.Name);
                    Require(selected.clip != previous, "Immediate identical variant repeated: " + cue.Name);
                    previous = selected.clip;
                }
            }
            checks.Add("PASS: all " + SfxCatalog.All.Length + " cues route correctly and avoid immediate variant repeats (" + (SfxCatalog.All.Length*12) + " events).");
            ResetVoices(voices, banks);
            for (int n = 0; n < 1000; n++)
            {
                foreach (DictionaryEntry entry in banks) entry.Value.GetType().GetField("next").SetValue(entry.Value, 0d);
                SoundManager.PlayCue(SfxCatalog.All[n % SfxCatalog.All.Length].Name, 2);
            }
            float total = 0;
            foreach (var source in sources) if (source.clip != null) total += source.volume;
            Require(total <= 1.2001f && test.GetComponents<AudioSource>().Length == 24, "Stress exceeded gain or source budget");
            checks.Add("PASS: 1,000 rapid events stay within 24 sources and the summed gain budget.");
            Require(JsonUtility.ToJson(randomState) == JsonUtility.ToJson(UnityEngine.Random.state), "SFX changed gameplay random state");
            checks.Add("PASS: audio variation preserves Unity gameplay random state.");
            ResetVoices(voices, banks);
            SoundManager.PlayCue("shot1", float.NaN);
            SoundManager.PlayCue("shot1", -1);
            SoundManager.PlaySoundPitch("shot1", .1f, float.PositiveInfinity);
            SoundManager.PlayCue("missing-cue");
            SoundManager.PlayCue(null);
            SoundManager.PlaySound(null, .1f);
            Require(Occupied(sources) == 0, "Invalid playback input was accepted");
            checks.Add("PASS: invalid and unknown playback inputs are ignored safely.");
            File.WriteAllLines("ArtSource/AudioReview/unity-verification.txt", checks);
            Debug.Log("PSX AUDIO VERIFICATION PASSED\n" + string.Join("\n", checks));
        }
        catch (Exception error)
        {
            checks.Add("FAIL: " + error);
            Directory.CreateDirectory("ArtSource/AudioReview");
            File.WriteAllLines("ArtSource/AudioReview/unity-verification.txt", checks);
            Debug.LogException(error);
        }
        finally { if (test != null) UnityEngine.Object.DestroyImmediate(test); }
    }

    static int Occupied(AudioSource[] sources) { int count = 0; foreach (var source in sources) if (source.clip != null) count++; return count; }
    static void ResetVoices(Array voices, IDictionary banks)
    {
        foreach (var voice in voices)
        {
            var type = voice.GetType();
            var source = (AudioSource)type.GetField("source").GetValue(voice);
            source.Stop(); source.clip = null;
            type.GetField("until").SetValue(voice, 0d);
        }
        foreach (DictionaryEntry entry in banks) entry.Value.GetType().GetField("next").SetValue(entry.Value, 0d);
    }

    static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
#endif
