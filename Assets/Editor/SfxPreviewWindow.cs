using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class SfxPreviewWindow : EditorWindow
{
    SfxMix mix;
    List<SfxPreviewLibrary.Cue> cues;
    string[] groups;
    [SerializeField] string selected = "enemySimple", search = "";
    [SerializeField] int group;
    [SerializeField] float repeatRate = 6;
    Vector2 listScroll, detailScroll;
    string status;
    string auditionCue;
    float auditionPeak;
    bool error;
    readonly Dictionary<string, AudioClip> defaults = new Dictionary<string, AudioClip>();

    [MenuItem("Tools/Shootemup/SFX Preview")]
    public static void Open() => GetWindow<SfxPreviewWindow>("SFX Preview");

    void OnEnable()
    {
        minSize = new Vector2(860, 520);
        Reload();
        Undo.undoRedoPerformed += UndoChanged;
        AssemblyReloadEvents.beforeAssemblyReload += SfxPreviewAudio.Stop;
        EditorApplication.playModeStateChanged += PlayModeChanged;
    }
    void OnDisable()
    {
        SfxPreviewAudio.Stop();
        Undo.undoRedoPerformed -= UndoChanged;
        AssemblyReloadEvents.beforeAssemblyReload -= SfxPreviewAudio.Stop;
        EditorApplication.playModeStateChanged -= PlayModeChanged;
    }
    void PlayModeChanged(PlayModeStateChange state) { SfxPreviewAudio.Stop(); defaults.Clear(); Repaint(); }
    void UndoChanged() { if (mix != null) { mix.Invalidate(); Save(); } Repaint(); }
    void Reload()
    {
        Guard(() => {
            mix = SfxPreviewLibrary.EnsureMix();
            cues = SfxPreviewLibrary.Load();
            groups = new[] { "All sounds" }.Concat(cues.Select(c => c.group).Distinct()).ToArray();
            group = Mathf.Clamp(group, 0, groups.Length - 1);
            defaults.Clear();
        });
    }
    void Save() { EditorUtility.SetDirty(mix); AssetDatabase.SaveAssetIfDirty(mix); }
    void Change(string id, Action<SfxMix.Entry> apply)
    {
        var previousClip = mix.Find(id)?.clip;
        Undo.RecordObject(mix, "Change SFX " + id);
        apply(mix.Edit(id));
        mix.Invalidate();
        Save();
        if (id == auditionCue)
        {
            if (previousClip != mix.Find(id)?.clip) { SfxPreviewAudio.Stop(); auditionCue = null; }
            else
            {
                var cue = cues.Find(c => c.id == id);
                float gain = cue.gameGain * ItemSound.Gain(id);
                SfxPreviewAudio.SetGain(gain);
                SetPeakStatus(cue.label, auditionPeak * gain);
            }
        }
    }
    AudioClip Current(string id)
    {
        var chosen = mix.Find(id)?.clip;
        if (chosen != null) return chosen;
        if (!defaults.TryGetValue(id, out var clip)) defaults[id] = clip = SfxPreviewLibrary.DefaultClip(id);
        return clip;
    }
    void Guard(Action action)
    {
        try { error = false; action(); }
        catch (Exception exception) { error = true; status = exception.GetBaseException().Message; }
    }
    void Audition(SfxPreviewLibrary.Cue cue, SfxPreviewLibrary.Take take, bool repeat)
    {
        Guard(() => {
            var source = take == null ? SfxPreviewAudio.FromClip(Current(cue.id)) : SfxPreviewAudio.ReadWave(File.ReadAllBytes(SfxPreviewLibrary.SamplePath(take)));
            var rendered = SfxPreviewAudio.Render(source, 1, cue.pitch, repeat, repeat && cue.Loop, repeatRate);
            float gain = cue.gameGain * ItemSound.Gain(cue.id);
            auditionCue = cue.id;
            auditionPeak = SfxPreviewAudio.Peak(rendered);
            SetPeakStatus((take?.label ?? cue.label) + " · " + source.Seconds.ToString("0.00") + " s", auditionPeak * gain);
            SfxPreviewAudio.Play(rendered, gain);
        });
    }
    void SetPeakStatus(string label, float peak)
    {
        status = label + " · " + (peak > 0 ? (20 * Mathf.Log10(peak)).ToString("0.0") + " dBFS peak" : "silent") +
            (peak > 1 ? " · clipping in this preview" : "");
        error = peak > 1;
    }
    void Use(SfxPreviewLibrary.Cue cue, SfxPreviewLibrary.Take take)
    {
        Guard(() => {
            var clip = SfxPreviewLibrary.ImportTake(cue.id, take);
            Change(cue.id, entry => { entry.clip = clip; entry.choice = "Sound Lab " + (take.id ?? "original"); });
            status = "Saved · " + cue.label + " / " + take.label;
        });
    }

    void OnGUI()
    {
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
        { SfxPreviewAudio.Stop(); status = "Stopped"; Event.current.Use(); }
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            search = GUILayout.TextField(search, EditorStyles.toolbarSearchField, GUILayout.MinWidth(180));
            if (groups != null) group = EditorGUILayout.Popup(group, groups, EditorStyles.toolbarPopup, GUILayout.Width(180));
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(62))) Reload();
            if (GUILayout.Button("Stop", EditorStyles.toolbarButton, GUILayout.Width(52))) { SfxPreviewAudio.Stop(); status = "Stopped"; }
        }
        if (cues == null || mix == null) { EditorGUILayout.HelpBox(status ?? "Audio library unavailable.", MessageType.Error); return; }
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(345)))
            {
                listScroll = EditorGUILayout.BeginScrollView(listScroll);
                foreach (var cue in cues)
                {
                    if (group > 0 && cue.group != groups[group]) continue;
                    if (search.Length > 0 && (cue.label + " " + cue.id).IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        bool isSelected = cue.id == selected;
                        if (GUILayout.Toggle(isSelected, cue.label, "Button", GUILayout.Height(25), GUILayout.ExpandWidth(true)) && !isSelected)
                            selected = cue.id;
                        var entry = mix.Find(cue.id);
                        string level = entry?.muted == true ? "Mute" : ((entry?.gain ?? 1) * 100).ToString("0") + "%";
                        GUILayout.Label(level, EditorStyles.miniLabel, GUILayout.Width(40));
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            using (new EditorGUILayout.VerticalScope())
            {
                detailScroll = EditorGUILayout.BeginScrollView(detailScroll);
                var cue = cues.Find(c => c.id == selected);
                if (cue != null) DrawCue(cue);
                EditorGUILayout.EndScrollView();
            }
        }
        if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, error ? MessageType.Warning : MessageType.None);
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("Autosaved · 100% = authored volume", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Import saved Sound Lab picks", EditorStyles.toolbarButton))
                Guard(() => {
                    int count = SfxPreviewLibrary.ImportSavedPicks(mix, cues, Path.GetFullPath(Path.Combine(Application.dataPath, "../SoundLab/selections.json")));
                    status = count > 0 ? "Imported " + count + " saved choices." : "No chosen takes to import. Pending / rejected picks stay unchanged.";
                    defaults.Clear();
                });
        }
    }
    void DrawCue(SfxPreviewLibrary.Cue cue)
    {
        EditorGUILayout.Space(8);
        GUILayout.Label(cue.label, EditorStyles.boldLabel);
        GUILayout.Label(cue.group, EditorStyles.miniLabel);
        var entry = mix.Find(cue.id);
        EditorGUI.BeginChangeCheck();
        float volume = EditorGUILayout.Slider("Volume %", (entry?.gain ?? 1) * 100, 0, 200);
        bool muted = EditorGUILayout.Toggle("Mute", entry?.muted ?? false);
        if (EditorGUI.EndChangeCheck()) Change(cue.id, e => { e.gain = volume / 100; e.muted = muted; });
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Play once", GUILayout.Height(29))) Audition(cue, null, false);
            if (GUILayout.Button(cue.Loop ? "Play loop · 6 s" : "Repeat · 6 s", GUILayout.Height(29))) Audition(cue, null, true);
        }
        repeatRate = EditorGUILayout.Slider("Shots / second", repeatRate, 1, 20);
        var clip = Current(cue.id);
        EditorGUI.BeginChangeCheck();
        var replacement = (AudioClip)EditorGUILayout.ObjectField("Current sound", clip, typeof(AudioClip), false);
        if (EditorGUI.EndChangeCheck())
            Change(cue.id, e => { e.clip = replacement; e.choice = replacement != null ? "Custom" : null; });
        if (clip == null) GUILayout.Label("Currently silent — choose a take below.", EditorStyles.miniLabel);
        if (entry?.clip != null)
        {
            GUILayout.Label(entry.choice ?? "Custom", EditorStyles.miniLabel);
            if (GUILayout.Button("Use default sound")) Change(cue.id, e => { e.clip = null; e.choice = null; });
        }
        EditorGUILayout.Space(12);
        GUILayout.Label("Sound Lab takes", EditorStyles.boldLabel);
        if (cue.original != null) DrawTake(cue, cue.original, "Original recording");
        foreach (var take in cue.choices ?? Array.Empty<SfxPreviewLibrary.Take>()) DrawTake(cue, take, take.label);
        if (cue.original == null && (cue.choices == null || cue.choices.Length == 0))
            GUILayout.Label("Drop an AudioClip above to choose another sound.", EditorStyles.miniLabel);
    }
    void DrawTake(SfxPreviewLibrary.Cue cue, SfxPreviewLibrary.Take take, string label)
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            GUILayout.Label(new GUIContent(label, take.description), GUILayout.MinWidth(110), GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Play", GUILayout.Width(44))) Audition(cue, take, false);
            if (GUILayout.Button("Repeat", GUILayout.Width(56))) Audition(cue, take, true);
            if (GUILayout.Button("Use", GUILayout.Width(42))) Use(cue, take);
        }
    }
}
