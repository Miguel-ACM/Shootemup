using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Run in a disposable headless project. It never opens or controls Miguel's live editor.
[InitializeOnLoad]
public static class VerifySfxPreview
{
    const string Key = "VerifySfxPreview";
    public const string Folder = "ReviewOutput/SfxPreview";
    static double deadline;
    static VerifySfxPreview()
    {
        EditorApplication.playModeStateChanged += state => {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
            SessionState.SetBool(Key, false);
            deadline = EditorApplication.timeSinceStartup + 90;
            new GameObject("SFX routing regression").AddComponent<SfxPreviewProbe>();
        };
        EditorApplication.update += () => {
            if (deadline == 0 || EditorApplication.timeSinceStartup < deadline) return;
            Finish(new Exception("Runtime checks timed out."));
        };
    }
    public static void Check(bool pass, string message)
    {
        File.AppendAllText(Folder + "/Checks.txt", (pass ? "PASS " : "FAIL ") + message + "\n");
        if (!pass) throw new Exception(message);
    }
    static void Isolated()
    {
        if (!Application.isBatchMode || !Application.dataPath.Replace('\\', '/').Contains("/Temp/SfxPreviewReview/"))
            throw new InvalidOperationException("Use the isolated headless SfxPreviewReview project.");
    }
    public static void Run()
    {
        Isolated();
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Folder + "/Checks.txt", "");
        var mix = SfxPreviewLibrary.EnsureMix();
        File.WriteAllText(Folder + "/OriginalMix.json", EditorJsonUtility.ToJson(mix));
        try
        {
            var cues = SfxPreviewLibrary.Load();
            Check(cues.Count >= 80 && cues.Select(c => c.id).Distinct().Count() == cues.Count, "Complete catalogue with unique cue IDs: " + cues.Count);
            Check(SfxPreviewAudio.Available, "Installed Unity editor exposes the audio preview API");
            int count = 0;
            foreach (var cue in cues)
                foreach (var take in (cue.choices ?? Array.Empty<SfxPreviewLibrary.Take>()).Concat(cue.original != null ? new[] { cue.original } : Array.Empty<SfxPreviewLibrary.Take>()))
                {
                    var pcm = SfxPreviewAudio.ReadWave(File.ReadAllBytes(SfxPreviewLibrary.SamplePath(take)));
                    if (pcm.samples.Length == 0 || !float.IsFinite(SfxPreviewAudio.Peak(pcm))) throw new Exception("Bad PCM: " + take.file);
                    count++;
                }
            Check(count >= 250, "Decoded existing Sound Lab WAVs without importing them: " + count);
            foreach (var cue in SoundCueCatalog.All)
                Check(SfxPreviewLibrary.DefaultClip(cue.Id) != null, "Default recording exists: " + cue.Id);
            var source = SfxPreviewAudio.FromClip(SfxPreviewLibrary.DefaultClip("enemySimple"));
            var half = SfxPreviewAudio.Render(source, .5f, 1, false, false, 6);
            Check(Mathf.Abs(SfxPreviewAudio.Peak(half) - SfxPreviewAudio.Peak(source) * .5f) < .00001f, "Preview gain preserves amplitude (-6.02 dB at 50%)");
            var silent = SfxPreviewAudio.Render(source, 0, 1, true, false, 20);
            Check(SfxPreviewAudio.Peak(silent) == 0, "Muted repeated preview is silent");
            var impulse = new SfxPreviewAudio.Pcm { samples = new float[] { 1, -.5f }, channels = 2, rate = 8000 };
            var repeated = SfxPreviewAudio.Render(impulse, .4f, 1, true, false, 10);
            Check(repeated.samples[0] == .4f && repeated.samples[1] == -.2f && repeated.samples[1600] == .4f && repeated.samples[800] == 0,
                "Repeat preview preserves stereo and requested event spacing");
            bool rejected = false;
            try { SfxPreviewAudio.ReadWave(new byte[64]); } catch (IOException) { rejected = true; }
            Check(rejected, "Malformed audio is rejected before import");
            string picks = Path.GetFullPath("SoundLab/selections.json");
            string before = File.ReadAllText(picks);
            string fixture = Folder + "/Picks.json";
            File.WriteAllText(fixture, "{\"picks\":{\"shot1\":{\"choice\":\"pending\",\"gain\":0.1},\"enemyBurst\":{\"choice\":\"reject\",\"gain\":0},\"enemyNeedle\":{\"choice\":\"a\",\"gain\":0.4}}}");
            int imported = SfxPreviewLibrary.ImportSavedPicks(mix, cues, fixture);
            Check(imported == 1 && mix.Find("enemyNeedle")?.clip != null && mix.Find("shot1") == null && mix.Find("enemyBurst") == null,
                "Saved choice importer applies chosen take / gain and skips pending or rejected picks");
            Check(before == File.ReadAllText(picks), "Original Sound Lab selections remain byte-for-byte unchanged");
            var selected = mix.Find("enemyNeedle").clip;
            Check(SfxPreviewLibrary.ImportTake("enemyNeedle", cues.Find(c => c.id == "enemyNeedle").choices[0]) == selected,
                "Repeated import reuses the immutable clip asset");
            mix.Edit("enemySimple").gain = .5f;
            mix.Edit("shotAK45").clip = selected;
            mix.Edit("shotAK45").gain = .3f;
            mix.Edit("itemBurn").clip = selected;
            mix.Edit("itemBurn").gain = .25f;
            mix.Edit("beamSustain").gain = .5f;
            mix.Edit("playerBeamSustain").gain = .25f;
            mix.Edit("beamCharge").gain = .5f;
            mix.Edit("ricochetHit").muted = true;
            mix.Invalidate();
            EditorUtility.SetDirty(mix);
            AssetDatabase.SaveAssetIfDirty(mix);
            File.WriteAllText(Folder + "/RuntimeMix.json", EditorJsonUtility.ToJson(mix));
            PlayerSettings.companyName = "ShootemupReview";
            PlayerSettings.productName = "SfxPreviewReview";
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SessionState.SetBool(Key, true);
            EditorApplication.isPlaying = true;
        }
        catch (Exception exception) { Finish(exception); }
    }
    public static void RunRuntime()
    {
        Isolated();
        var mix = SfxPreviewLibrary.EnsureMix();
        File.AppendAllText(Folder + "/Checks.txt", "\nRUNTIME RETEST\n");
        // Re-run only gameplay routing after an editor-library check has already passed.
        var selected = SfxPreviewLibrary.ImportTake("enemyNeedle", SfxPreviewLibrary.Load().Find(c => c.id == "enemyNeedle").choices[0]);
        mix.Edit("enemySimple").gain = .5f;
        mix.Edit("enemyNeedle").clip = selected; mix.Edit("enemyNeedle").gain = .4f;
        mix.Edit("shotAK45").clip = selected; mix.Edit("shotAK45").gain = .3f;
        mix.Edit("itemBurn").clip = selected; mix.Edit("itemBurn").gain = .25f;
        mix.Edit("beamSustain").gain = .5f; mix.Edit("playerBeamSustain").gain = .25f;
        mix.Edit("beamCharge").gain = .5f; mix.Edit("ricochetHit").muted = true;
        mix.Invalidate(); EditorUtility.SetDirty(mix); AssetDatabase.SaveAssetIfDirty(mix);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Key, true);
        EditorApplication.isPlaying = true;
    }
    public static void Finish(Exception failure)
    {
        deadline = 0;
        var mix = SfxPreviewLibrary.EnsureMix();
        string original = Folder + "/OriginalMix.json";
        if (File.Exists(original))
        {
            EditorJsonUtility.FromJsonOverwrite(File.ReadAllText(original), mix);
            mix.Invalidate();
            EditorUtility.SetDirty(mix);
            AssetDatabase.SaveAssetIfDirty(mix);
        }
        File.WriteAllText(Folder + "/Result.txt", failure == null ? "PASS\n" : "FAIL\n" + failure);
        EditorApplication.Exit(failure == null ? 0 : 1);
    }
    public static void Build()
    {
        Isolated();
        if (!File.ReadAllText(Folder + "/Result.txt").StartsWith("PASS")) throw new Exception("Run SFX checks first.");
        try
        {
            var pcm = new SfxPreviewAudio.Pcm { samples = Enumerable.Repeat(.4f, 800).ToArray(), channels = 1, rate = 8000 };
            var control = AudioClip.Create("Headless PCM control", 800, 1, 8000, false);
            bool wrote = control.SetData(pcm.samples, 0);
            var controlData = new float[800];
            bool read = control.GetData(controlData, 0);
            bool nativeReadable = wrote && read && Mathf.Abs(controlData[0] - .4f) < .00005f;
            File.AppendAllText(Folder + "/Checks.txt", "Native PCM control: write=" + wrote + "; read=" + read + "; sample=" + controlData[0] + "\n");
            UnityEngine.Object.DestroyImmediate(control);
            SfxPreviewAudio.Play(pcm, .5f);
            var clip = (AudioClip)typeof(SfxPreviewAudio).GetField("playing", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            var data = new float[800];
            clip.GetData(data, 0);
            var buffer = typeof(SfxPreviewAudio).GetField("scaled", BindingFlags.NonPublic | BindingFlags.Static);
            // Unity's native clip buffer may quantize float samples to PCM16.
            Check(Mathf.Abs(((float[])buffer.GetValue(null))[0] - .2f) < .00001f, "Preview gain fills the selected amplitude");
            if (nativeReadable) Check(Mathf.Abs(data[0] - .2f) < .00005f, "Native preview starts at the selected volume; sample=" + data[0]);
            SfxPreviewAudio.SetGain(0);
            clip.GetData(data, 0);
            Check(((float[])buffer.GetValue(null)).All(value => value == 0), "Preview mute updates the existing buffer");
            if (nativeReadable) Check(data.All(value => value == 0), "Native preview mute updates the existing audio buffer");
            SfxPreviewAudio.SetGain(1.5f);
            clip.GetData(data, 0);
            Check(Mathf.Abs(((float[])buffer.GetValue(null))[0] - .6f) < .00001f, "Preview gain restores without compounding or restarting");
            if (nativeReadable) Check(Mathf.Abs(data[0] - .6f) < .00005f, "Native preview restores gain without compounding or restarting; sample=" + data[0]);
            else File.AppendAllText(Folder + "/Checks.txt", "NOT VERIFIED: headless native audio readback returns zero even for the independent PCM control; no listening / graphical review.\n");
        }
        finally { SfxPreviewAudio.Stop(); }
        PlayerSettings.companyName = "DefaultCompany";
        PlayerSettings.productName = "seup";
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            target = BuildTarget.StandaloneWindows64,
            locationPathName = Folder + "/Player/Shootemup.exe"
        });
        bool pass = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
        File.WriteAllText(Folder + "/Build.txt", (pass ? "PASS" : "FAIL") + "\nErrors=" + report.summary.totalErrors + "; warnings=" + report.summary.totalWarnings);
        EditorApplication.Exit(pass ? 0 : 1);
    }
}

public sealed class SfxPreviewProbe : MonoBehaviour
{
    const BindingFlags StaticHidden = BindingFlags.Static | BindingFlags.NonPublic;
    static int Events => (int)typeof(SoundManager).GetField("nextEnemySource", StaticHidden).GetValue(null);
    static AudioSource[] Voices => (AudioSource[])typeof(SoundManager).GetField("enemySources", StaticHidden).GetValue(null);
    void Awake()
    {
        UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = null;
        QualitySettings.renderPipeline = null;
        Time.timeScale = 1;
        AudioListener.pause = false;
    }
    IEnumerator Start()
    {
        var manager = new GameObject("Sound manager", typeof(AudioSource), typeof(SoundManager));
        yield return null;
        Exception failure = null;
        try { Verify(manager); } catch (Exception exception) { failure = exception; }
        VerifySfxPreview.Finish(failure);
    }
    void Verify(GameObject manager)
    {
        VerifySfxPreview.Check(SfxMix.Gain("enemySimple") == .5f && SfxMix.Gain("ricochetHit") == 0,
            "Mix gains and mute survive editor-to-runtime domain reload");
        VerifySfxPreview.Check(SfxMix.Override("enemyNeedle") != null && SfxMix.Gain("enemyNeedle") == .4f,
            "Selected Sound Lab clip and volume survive reload");
        var state = UnityEngine.Random.state;
        var actualRound = CombatRound.Launch<SimpleRound>(Vector3.zero, Vector2.down);
        VerifySfxPreview.Check(actualRound != null && Events == 1, "Actual projectile launch keeps its default sound after a volume-only edit");
        for (int i = 0; i < 1000; i++) SoundManager.PlayEnemyLaunch(typeof(NeedleRound), Vector3.zero);
        VerifySfxPreview.Check(Events == 1 && Voices.Length == 6 && manager.GetComponents<AudioSource>().Length == 11,
            "Actual round launch plus 1000 same-frame events uses one accent and six fixed enemy voices");
        VerifySfxPreview.Check(Mathf.Abs(Voices[0].volume - .0175f) < .00001f,
            "Enemy launch applies authored level times saved gain; actual=" + Voices[0].volume + "; clip=" + Voices[0].clip?.name + "; pitch=" + Voices[0].pitch);
        state = UnityEngine.Random.state;
        SoundManager.PlayEnemyEvent("shellBurst");
        VerifySfxPreview.Check(Events == 2 && UnityEngine.Random.state.Equals(state),
            "Shell detonation remains audible beside its child volley without consuming combat RNG");
        SoundManager.PlayEnemyEvent("ricochetHit");
        SoundManager.PlayEnemyLaunch(typeof(PulseLaser), Vector3.zero);
        VerifySfxPreview.Check(Events == 2, "Muted impacts and attached lasers add no duplicate launch voice");
        var get = typeof(SoundManager).GetMethod("GetClip", StaticHidden);
        VerifySfxPreview.Check((AudioClip)get.Invoke(null, new object[] { "shotAK45" }) == SfxMix.Override("shotAK45"),
            "Player weapon override wins over the existing sound bank");
        ItemSound.Play("itemBurn", .2f, 1, 0);
        var items = FindAnyObjectByType<ItemSound>();
        var itemVoice = items.GetComponents<AudioSource>().First(v => v.clip != null);
        VerifySfxPreview.Check(itemVoice.clip == SfxMix.Override("itemBurn") && Mathf.Abs(itemVoice.volume - .05f) < .00001f,
            "Item playback uses chosen clip and per-event gain");
        var beam = new GameObject("Beam sound").AddComponent<LaserSound>();
        beam.Charge(1);
        var sources = beam.GetComponents<AudioSource>();
        VerifySfxPreview.Check(Mathf.Abs(sources[0].volume - .1f) < .00001f, "Enemy laser charge gain is routed");
        beam.Drive(1, true);
        VerifySfxPreview.Check(Mathf.Abs(sources[1].volume - .075f) < .00001f, "Enemy sustain gain is routed");
        SfxMix.Current.Edit("beamSustain").gain = .2f;
        beam.Drive(1, true);
        VerifySfxPreview.Check(Mathf.Abs(sources[1].volume - .03f) < .00001f, "Loop volume responds while the beam is held");
        beam.Stop();
        beam.Charge(1, true);
        beam.Drive(1, true);
        VerifySfxPreview.Check(Mathf.Abs(sources[1].volume - .0275f) < .00001f, "Player and enemy beam levels remain independent");
        beam.Stop();
        Time.timeScale = 0;
        typeof(SoundManager).GetField("nextEnemyEvent", StaticHidden).SetValue(null, 0f);
        SoundManager.PlayEnemyLaunch(typeof(GiantOrb), Vector3.zero);
        VerifySfxPreview.Check(Events == 2, "Paused combat cannot emit new projectile sounds");
    }
}
