#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public sealed class EnemyEditorWindow : EditorWindow
{
    public const string AssetPath = "Assets/Resources/EnemyTuning.asset";
    public sealed class EnemyInfo
    {
        public Type type; public LevelId level; public bool boss; public string Name => WatcherCatalog.Pretty(type.Name);
    }

    public sealed class ValueInfo
    {
        public string key; public FieldInfo field; public EnemyTunableAttribute range; public float baseline;
    }

    static List<EnemyInfo> catalog;
    [SerializeField] string selected = "ThunderLanceBoss", search = "";
    [SerializeField] EnemyTuningProfile draft;
    [SerializeField] bool dirty, phaseTwo;
    Vector2 listScroll, settingsScroll, orbit;
    float zoom = 1;
    EnemyInfo info;
    List<ValueInfo> values;
    Mesh mesh;
    Material material;
    PreviewRenderUtility preview;
    EnemyTuning asset;
    public static IReadOnlyList<EnemyInfo> Catalog
    {
        get
        {
            if (catalog != null)
                return catalog;
            catalog = new List<EnemyInfo>();
            var bosses = new[] { typeof(FieldsBomberBoss), typeof(TunnelSawBoss), typeof(SnowSiegeBoss), typeof(ThunderLanceBoss), typeof(CityDuoBoss), typeof(DesertWormBoss), typeof(CandyRegentBoss), typeof(SpaceCitadelBoss), typeof(AbyssSubmarineBoss), typeof(HellCrabBoss) };
            foreach (LevelId level in Enum.GetValues(typeof(LevelId)))
            {
                foreach (var type in WatcherCatalog.Enemies(level))
                    catalog.Add(new EnemyInfo { type = type, level = level });
                catalog.Add(new EnemyInfo { type = bosses[(int)level], level = level, boss = true });
            }
            return catalog;
        }
    }

    [MenuItem("Tools/Shootemup/Enemy Editor")]
    public static EnemyEditorWindow Open()
    {
        var window = GetWindow<EnemyEditorWindow>("Enemy Editor");
        window.minSize = new Vector2(820, 560);
        window.Show();
        return window;
    }

    public static EnemyTuning EnsureAsset()
    {
        var result = AssetDatabase.LoadAssetAtPath<EnemyTuning>(AssetPath);
        if (result != null)
            return result;
        result = CreateInstance<EnemyTuning>();
        AssetDatabase.CreateAsset(result, AssetPath);
        AssetDatabase.SaveAssets();
        return result;
    }

    void OnEnable()
    {
        asset = EnsureAsset();
        Select(selected, false);
        Undo.undoRedoPerformed += UndoChanged;
    }

    void OnDisable()
    {
        Undo.undoRedoPerformed -= UndoChanged;
        preview?.Cleanup();
        preview = null;
    }

    void UndoChanged()
    {
        Select(selected, false);
        Repaint();
    }

    public void Select(string name, bool keepDraft = false)
    {
        info = Catalog.FirstOrDefault(e => e.type.Name == name) ?? Catalog[0];
        selected = info.type.Name;
        values = new List<ValueInfo>();
        var model = DistrictEnemy.Spawn(info.type, Vector3.zero, false, false, false);
        try
        {
            mesh = model.Hull.GetComponent<MeshFilter>().sharedMesh;
            material = model.Hull.GetComponent<MeshRenderer>().sharedMaterial;
            if (model is DesertWormBoss)
                material = IndustrialMeshFactory.WhiteWormMaterial;
            AddValues(model, "");
            var mover = model.GetComponent<PatternMover>();
            if (mover != null)
                AddValues(mover, "mover.");
        }
        finally { DestroyImmediate(model.gameObject); }
        if (keepDraft && draft != null && draft.enemy == selected)
            return;
        var saved = asset.Find(selected);
        draft = saved != null ? JsonUtility.FromJson<EnemyTuningProfile>(JsonUtility.ToJson(saved)) : new EnemyTuningProfile { enemy = selected };
        foreach (var value in values)
            if (!draft.values.Any(v => v.field == value.key))
                draft.values.Add(new EnemyValueOverride { field = value.key, value = value.baseline });
        dirty = false;
        orbit = Vector2.zero;
        zoom = 1;
        settingsScroll = Vector2.zero;
    }

    void AddValues(object target, string prefix)
    {
        foreach (var field in EnemyTuning.Fields(target.GetType()))
            values.Add(new ValueInfo
            {
                key = prefix + field.Name,
                field = field,
                range = (EnemyTunableAttribute)Attribute.GetCustomAttribute(field, typeof(EnemyTunableAttribute)),
                baseline = Convert.ToSingle(field.GetValue(target))
            });
    }

    public void Apply()
    {
        Undo.RecordObject(asset, "Tune " + selected);
        var copy = JsonUtility.FromJson<EnemyTuningProfile>(JsonUtility.ToJson(draft));
        copy.movement = Mathf.Clamp(copy.movement, .35f, 2.5f);
        copy.projectileSpeed = Mathf.Clamp(copy.projectileSpeed, .25f, 3);
        copy.projectileDamage = Mathf.Clamp(copy.projectileDamage, .1f, 5);
        foreach (var value in values)
        {
            var entry = copy.values.First(v => v.field == value.key);
            entry.value = Mathf.Clamp(float.IsFinite(entry.value) ? entry.value : value.baseline, value.range.Minimum, value.range.Maximum);
            if (value.field.FieldType == typeof(int))
                entry.value = Mathf.Round(entry.value);
        }
        int index = asset.enemies.FindIndex(p => p.enemy == selected);
        if (index < 0)
            asset.enemies.Add(copy);
        else
            asset.enemies[index] = copy;
        asset.Changed();
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssetIfDirty(asset);
        dirty = false;
    }

    void ResetSelected()
    {
        Undo.RecordObject(asset, "Reset " + selected);
        asset.enemies.RemoveAll(p => p.enemy == selected);
        asset.Changed();
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssetIfDirty(asset);
        Select(selected, false);
    }

    void OnGUI()
    {
        if (info == null || asset == null)
        {
            asset = EnsureAsset();
            Select(selected, false);
        }
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("ENEMY EDITOR", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Tuning asset", EditorStyles.toolbarButton))
                EditorGUIUtility.PingObject(asset);
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(218)))
            {
                search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
                listScroll = EditorGUILayout.BeginScrollView(listScroll);
                foreach (LevelId level in Enum.GetValues(typeof(LevelId)))
                {
                    var group = Catalog.Where(e => e.level == level && (e.Name + " " + level).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
                    if (group.Length == 0)
                        continue;
                    GUILayout.Space(7);
                    GUILayout.Label(level == LevelId.Fields ? "PLAINS" : level.ToString().ToUpperInvariant(), EditorStyles.miniBoldLabel);
                    foreach (var enemy in group)
                    {
                        bool chosen = enemy.type.Name == selected;
                        if (GUILayout.Toggle(chosen, (enemy.boss ? "\u25c6 " : "  ") + enemy.Name, "Button") && !chosen)
                        {
                            if (dirty)
                                Apply();
                            Select(enemy.type.Name);
                            GUIUtility.ExitGUI();
                        }
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            using (new EditorGUILayout.VerticalScope())
            {
                GUILayout.Label(info.Name, EditorStyles.largeLabel);
                DrawPreview(GUILayoutUtility.GetRect(180, 210, GUILayout.ExpandWidth(true)));
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(EditorApplication.isPlaying && WaveWatcher.Current == null))
                        if (GUILayout.Button("Playtest in Wave Watcher"))
                        {
                            if (dirty)
                                Apply();
                            EnemyEditorPlaytest.Request(info, phaseTwo);
                        }
                    if (info.boss)
                        phaseTwo = GUILayout.Toggle(phaseTwo, "Phase 2", GUILayout.Width(75));
                }
                settingsScroll = EditorGUILayout.BeginScrollView(settingsScroll);
                EditorGUI.BeginChangeCheck();
                draft.enabled = EditorGUILayout.Toggle("Use saved overrides", draft.enabled);
                using (new EditorGUI.DisabledScope(!draft.enabled))
                {
                    draft.movement = EditorGUILayout.Slider("Movement rate", draft.movement, .35f, 2.5f);
                    draft.projectileSpeed = EditorGUILayout.Slider("Direct shot speed", draft.projectileSpeed, .25f, 3);
                    draft.projectileDamage = EditorGUILayout.Slider("Direct shot damage", draft.projectileDamage, .1f, 5);
                    GUILayout.Space(5);
                    GUILayout.Label("Override only the values you change", EditorStyles.miniLabel);
                    string section = "";
                    foreach (var value in values.OrderBy(v => v.range.Label))
                    {
                        var split = value.range.Label.Split('/');
                        string heading = split.Length > 1 ? split[0].Trim() : "General";
                        if (heading != section)
                        {
                            section = heading;
                            GUILayout.Space(7);
                            GUILayout.Label(section, EditorStyles.boldLabel);
                        }
                        var entry = draft.values.First(v => v.field == value.key);
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            bool wasEnabled = entry.enabled;
                            entry.enabled = EditorGUILayout.Toggle(entry.enabled, GUILayout.Width(18));
                            if (entry.enabled && !wasEnabled)
                                entry.value = value.baseline;
                            using (new EditorGUI.DisabledScope(!entry.enabled))
                            {
                                string label = split[split.Length - 1].Trim();
                                float number = entry.enabled ? entry.value : value.baseline;
                                number = value.field.FieldType == typeof(int) ? EditorGUILayout.DelayedIntField(label, Mathf.RoundToInt(number)) : EditorGUILayout.DelayedFloatField(label, number);
                                if (entry.enabled)
                                    entry.value = Mathf.Clamp(number, value.range.Minimum, value.range.Maximum);
                            }
                        }
                    }
                    bool formations = !info.boss && WavePartLibrary.For(info.level).Any(p => p.Enemy == selected);
                    if (formations)
                    {
                        GUILayout.Space(9);
                        draft.useFormationScore = EditorGUILayout.Toggle("Override formation accents", draft.useFormationScore);
                    }
                    if (formations && draft.useFormationScore)
                    {
                        GUILayout.Label("Flight fractions: 0.15-0.78. Shared by the whole formation.", EditorStyles.miniLabel);
                        int count = Mathf.Clamp(EditorGUILayout.DelayedIntField("Accent count", draft.formationAccents?.Length ?? 0), 1, 12);
                        if (draft.formationAccents == null || count != draft.formationAccents.Length)
                            Array.Resize(ref draft.formationAccents, count);
                        for (int i = 0; i < count; i++)
                            draft.formationAccents[i] = EditorGUILayout.Slider("Beat " + (i + 1), draft.formationAccents[i], .15f, .78f);
                    }
                }
                if (EditorGUI.EndChangeCheck())
                    dirty = true;
                GUILayout.Space(7);
                EditorGUILayout.HelpBox("Apply saves to the asset. New spawns use the values; run/miniboss modifiers still apply. Authored waves use their formation accents rather than the standalone attack interval.", MessageType.None);
                EditorGUILayout.EndScrollView();
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(!dirty))
                        if (GUILayout.Button("Apply"))
                            Apply();
                    if (GUILayout.Button("Revert draft"))
                        Select(selected, false);
                    if (GUILayout.Button("Reset enemy"))
                        ResetSelected();
                }
            }
        }
    }

    void DrawPreview(Rect rect)
    {
        if (mesh == null || material == null)
            return;
        var evt = Event.current;
        if (rect.Contains(evt.mousePosition))
        {
            if (evt.type == EventType.MouseDrag)
            {
                orbit += evt.delta * .5f;
                orbit.y = Mathf.Clamp(orbit.y, -45, 45);
                evt.Use();
                Repaint();
            }
            if (evt.type == EventType.ScrollWheel)
            {
                zoom = Mathf.Clamp(zoom - evt.delta.y * .05f, .5f, 2);
                evt.Use();
                Repaint();
            }
        }
        if (evt.type != EventType.Repaint)
            return;
        var texture = RenderModel(rect);
        GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, false);
    }

    Texture RenderModel(Rect rect)
    {
        if (preview == null)
            preview = new PreviewRenderUtility();
        preview.BeginPreview(rect, GUIStyle.none);
        var camera = preview.camera;
        camera.orthographic = true;
        camera.orthographicSize = Mathf.Max(.7f, mesh.bounds.extents.magnitude) * 1.15f / zoom;
        camera.transform.SetPositionAndRotation(new Vector3(0, 0, -15), Quaternion.identity);
        camera.nearClipPlane = .1f;
        camera.farClipPlane = 50;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.025f, .035f, .05f);
        preview.lights[0].intensity = 1;
        preview.lights[0].transform.rotation = Quaternion.Euler(25, 35, 0);
        preview.lights[1].intensity = .35f;
        var rotation = Quaternion.Euler(orbit.y, -orbit.x, 0);
        var matrix = Matrix4x4.TRS(-(rotation * mesh.bounds.center), rotation, Vector3.one);
        var resolution = Shader.GetGlobalVector("_PSXSnapResolution");
        Shader.SetGlobalVector("_PSXSnapResolution", new Vector4(rect.width, rect.height, 0, 0));
        try
        {
            preview.DrawMesh(mesh, matrix, material, 0);
            preview.Render(true);
            return preview.EndPreview();
        }
        finally { Shader.SetGlobalVector("_PSXSnapResolution", resolution); }
    }

    public void CaptureModel(string path)
    {
        var texture = RenderModel(new Rect(0, 0, 480, 360));
        var target = RenderTexture.GetTemporary(480, 360);
        var previous = RenderTexture.active;
        Graphics.Blit(texture, target);
        RenderTexture.active = target;
        var image = new Texture2D(480, 360, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 480, 360), 0, 0);
        image.Apply();
        File.WriteAllBytes(path, image.EncodeToPNG());
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(target);
        DestroyImmediate(image);
    }
}
#endif
