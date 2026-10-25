using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Installs the Blender-built PSXv2 fleet (Assets/Art/PSXv2, see ArtSource/PSXv2/README.md):
/// importer settings, materials, the player visual with an always-on-top hitbox in L1 and
/// PSX_Showcase, animated enemy visuals in the four enemy prefabs, then a play-mode capture.
/// Run from the menu or by creating ArtSource/PSXv2/install.request.
/// </summary>
[InitializeOnLoad]
public static class InstallPSXv2
{
    const string Folder = "Assets/Art/PSXv2/";
    const string Request = "ArtSource/PSXv2/install.request";
    const string CaptureRequest = "ArtSource/PSXv2/capture.request", CancelRequest = "ArtSource/PSXv2/capture.cancel", DiscardOk = "ArtSource/PSXv2/discard-unsaved.ok";
    const string CaptureScene = "Assets/Scenes/L1.unity";   // level 1 shows the grass field and real waves
    const string ReportPath = "ArtSource/PSXv2/InstallReport.json";
    const string CaptureFolder = "ArtSource/PSXv2/UnityCapture";
    const string StageKey = "PSXv2CaptureStage", StartKey = "PSXv2CaptureStart";
    static readonly string[] Scenes = { "Assets/Scenes/L1.unity", "Assets/Scenes/PSX_Showcase.unity" };
    static readonly (string prefab, string model)[] Enemies =
    {
        ("enemy1","PSXv2_Enemy_0_Dart"),("waver","PSXv2_Enemy_1_Rotor"),
        ("Biggie","PSXv2_Enemy_2_Gunboat"),("Bosses/stage1boss","PSXv2_Enemy_3_Warden"),
    };
    // Blender-space facts from build_ships.py: hexagon hitbox circumradius (pointy top) and centre, in world units.
    const float HitboxRadius = .08f;
    static readonly Vector2 HitboxCenter = new Vector2(0, .16f);
    static bool busy;

    static InstallPSXv2()
    {
        EditorApplication.update += Tick;
    }

    static void Tick()
    {
        if (busy || EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;
        // capture.cancel disarms a pending capture so it never interrupts someone else's Play session.
        if (File.Exists(CancelRequest))
        {
            File.Delete(CancelRequest);
            SessionState.SetInt(StageKey, 0);
            SessionState.SetBool("PSXv2Staged", false);
            return;
        }
        int stage = SessionState.GetInt(StageKey, 0);
        if (stage > 0)
        {
            busy = true;
            try
            {
                Capture(stage);
            }
            catch (Exception e) { Fail(e); SessionState.SetInt(StageKey, 0); }
            finally { busy = false; }
            return;
        }
        if ((File.Exists(CaptureRequest) || File.Exists(Request)) && !EditorApplication.isPlayingOrWillChangePlaymode && UnsavedWork())
            return;
        if (File.Exists(CaptureRequest) && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            File.Delete(CaptureRequest);
            Directory.CreateDirectory(CaptureFolder);
            EditorSceneManager.OpenScene(CaptureScene);
            SessionState.SetInt(StageKey, 1);
            SessionState.SetFloat(StartKey, 0);
            EditorApplication.isPlaying = true;
            return;
        }
        if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        busy = true;
        try
        {
            File.Delete(Request);
            Install();
        }
        catch (Exception e) { Fail(e); }
        finally { busy = false; }
    }

    /// <summary>Never open scenes over someone's unsaved edits: report and wait instead.</summary>
    static bool UnsavedWork()
    {
        // One-shot override, only after the owner confirmed the open scene holds none of their work.
        if (File.Exists(DiscardOk))
        {
            File.Delete(DiscardOk);
            return false;
        }
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            var scene = EditorSceneManager.GetSceneAt(i);
            if (!scene.isDirty)
                continue;
            File.WriteAllText("ArtSource/PSXv2/InstallError.txt", "Waiting: " + scene.path + " has unsaved changes in the editor.");
            return true;
        }
        return false;
    }

    static void Fail(Exception e)
    {
        File.WriteAllText("ArtSource/PSXv2/InstallError.txt", e.ToString());
        Debug.LogException(e);
    }

    [MenuItem("Tools/PSX Player/Install PSXv2 Blender fleet")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play mode first.");
        if (File.Exists("ArtSource/PSXv2/InstallError.txt"))
            File.Delete("ArtSource/PSXv2/InstallError.txt");
        var report = new StringBuilder("{");
        ConfigureTextures();
        ConfigureModels(report);
        var steel = Mat(Folder + "PSXv2 Steel.mat", "Shootemup/PSXv2 Lit");
        steel.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "PSXv2_Atlas.png");
        steel.SetFloat("_VertexSnap", 1);
        steel.SetFloat("_Affine", 1);
        steel.SetFloat("_Dither", 0);
        var core = Mat(Folder + "PSXv2 Hitbox Core.mat", "Shootemup/PSXv2 Hitbox");
        core.SetColor("_Color", new Color(1, .86f, .25f));   // bright yellow core
        var ring = Mat(Folder + "PSXv2 Hitbox Ring.mat", "Shootemup/PSXv2 Hitbox");
        ring.SetColor("_Color", new Color(.6f, .25f, .02f));   // dark orange outline inside the orange glow
        Directory.CreateDirectory("Assets/Resources/Effects");
        var fire = Mat("Assets/Resources/Effects/PSXv2Fireball.mat", "Shootemup/PSXv2 Fireball");
        fire.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "FX/Fireball_64.png");
        // Full PSX presentation by default, kept light: PS1 half-step dither, mild softness, no vignette.
        var screen = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/PSXPass/PSX screen composite.mat");
        if (screen != null)
        {
            screen.SetFloat("_Dither", .5f);
            screen.SetFloat("_Soften", .1f);
            screen.SetFloat("_Vignette", 0);
            EditorUtility.SetDirty(screen);
        }

        Orientation(out var rotation, out float scale, report);
        foreach (var path in Scenes)
            InstallPlayer(path, rotation, scale, steel, core, ring, report);
        InstallField(rotation, steel, report);
        report.Append("\"enemies\":[");
        for (int i = 0; i < Enemies.Length; i++)
        {
            var (prefab, model) = Enemies[i];
            if (i > 0)
                report.Append(',');
            Edit("Assets/Resources/Enemy/" + prefab + ".prefab", go => InstallEnemy(go, model, rotation, steel, report));
        }
        report.Append("],");
        InstallItems(rotation, steel, report);
        report.Append("}");
        AssetDatabase.SaveAssets();
        File.WriteAllText(ReportPath, report.ToString());
        Debug.Log("PSXv2 installed: " + report);
        // Play the showcase (autopilot + sample fleet) and capture a few frames.
        Directory.CreateDirectory(CaptureFolder);
        EditorSceneManager.OpenScene(CaptureScene);
        SessionState.SetInt(StageKey, 1);
        SessionState.SetFloat(StartKey, 0);
        EditorApplication.isPlaying = true;
    }

    // ------------------------------------------------------------------ importers
    static void ConfigureTextures()
    {
        var paths = new List<string> { Folder + "PSXv2_Atlas.png" };
        paths.AddRange(Directory.GetFiles(Folder + "FX", "*.png").Select(p => p.Replace('\\', '/')));
        paths.AddRange(Directory.GetFiles(Folder + "Field", "*.png").Select(p => p.Replace('\\', '/')));
        foreach (var path in paths)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = false;
            if (path.Contains("/FX/") || path.Contains("Grass"))
                importer.wrapMode = TextureWrapMode.Repeat;   // fireball skin scrolls, grass tiles
            if (path.Contains("Cloud"))
                importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }

    static void ConfigureModels(StringBuilder report)
    {
        foreach (var name in new[] { "PSXv2_Player", "PSXv2_Items", "PSXv2_Field" }.Concat(Enemies.Select(e => e.model)))
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(Folder + name + ".fbx");
            importer.globalScale = 1;
            importer.useFileScale = true;
            importer.bakeAxisConversion = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.None;
            importer.weldVertices = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importBlendShapes = false;
            bool animated = name.Contains("Enemy");
            importer.importAnimation = animated;
            importer.animationType = animated ? ModelImporterAnimationType.Legacy : ModelImporterAnimationType.None;
            importer.SaveAndReimport();
            if (animated)
            {
                var clips = importer.defaultClipAnimations;
                foreach (var clip in clips)
                {
                    clip.loopTime = true;
                    clip.wrapMode = WrapMode.Loop;
                }
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }
        }
    }

    static Material Mat(string path, string shader)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        var s = Shader.Find(shader);
        if (s == null)
            throw new InvalidOperationException("Missing shader " + shader);
        if (material == null)
        {
            material = new Material(s);
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = s;
        EditorUtility.SetDirty(material);
        return material;
    }


    // ------------------------------------------------------------------ orientation
    /// <summary>Finds where Blender's +Y (nose) and +Z (toward camera) land after FBX import, using
    /// the player's asymmetric extents, and the unit scale from the hitbox disc's known width.</summary>
    static void Orientation(out Quaternion rotation, out float scale, StringBuilder report)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "PSXv2_Player.fbx");
        var temp = UnityEngine.Object.Instantiate(asset);
        try
        {
            temp.transform.position = Vector3.zero;
            var renderers = temp.GetComponentsInChildren<MeshRenderer>();
            var hull = Bounds(renderers.Where(r => !r.name.Contains("Hitbox")));
            var disc = renderers.Single(r => r.name.Contains("HitboxCore")).bounds;
            int zAxis = Enumerable.Range(0, 3).OrderBy(i => hull.size[i]).First();
            int yAxis = Enumerable.Range(0, 3).Where(i => i != zAxis).OrderByDescending(i => Mathf.Abs(hull.center[i])).First();
            int xAxis = 3 - zAxis - yAxis;
            Vector3 up = Vector3.zero, toCamera = Vector3.zero;
            up[yAxis] = Mathf.Sign(hull.center[yAxis]);
            toCamera[zAxis] = Mathf.Sign(hull.center[zAxis]);
            // Unity: nose +Y, top toward the camera which looks along +Z, i.e. -Z.
            rotation = Quaternion.LookRotation(Vector3.back, Vector3.up) * Quaternion.Inverse(Quaternion.LookRotation(toCamera, up));
            scale = 2 * HitboxRadius * Mathf.Cos(Mathf.PI / 6) / disc.size[xAxis];   // flat-to-flat width of the hexagon
            report.AppendFormat(CultureInfo.InvariantCulture, "\"axes\":{{\"x\":{0},\"y\":{1},\"z\":{2},\"ySign\":{3},\"zSign\":{4}}},\"unitScale\":{5},\"rotation\":\"{6}\",",
                xAxis, yAxis, zAxis, up[yAxis], toCamera[zAxis], scale, rotation.eulerAngles);
        }
        finally { UnityEngine.Object.DestroyImmediate(temp); }
    }

    static Bounds Bounds(IEnumerable<Renderer> renderers)
    {
        var list = renderers.ToList();
        var b = list[0].bounds;
        foreach (var r in list.Skip(1))
            b.Encapsulate(r.bounds);
        return b;
    }

    static GameObject Instance(string model, Transform parent)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + model + ".fbx");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        return go;
    }

    static void SetLayer(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        foreach (Transform c in t)
            SetLayer(c, layer);
    }

    // ------------------------------------------------------------------ player
    static void InstallPlayer(string path, Quaternion rotation, float unitScale, Material steel, Material core, Material ring, StringBuilder report)
    {
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        var visual = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PSXShipVisual>(true)).Single();
        var ship = visual.transform;
        foreach (var name in new[] { "PSXv2 Player Visual", "PSXv2 Hitbox" })
        {
            var old = ship.Find(name);
            if (old != null)
                UnityEngine.Object.DestroyImmediate(old.gameObject);
        }
        var serialized = new SerializedObject(visual);
        var previous = serialized.FindProperty("model").objectReferenceValue as Transform;
        if (previous != null)
            previous.gameObject.SetActive(false);   // old GR-07 visual kept, disabled

        float local = unitScale / Mathf.Abs(ship.lossyScale.x);
        var holder = new GameObject("PSXv2 Player Visual").transform;
        holder.SetParent(ship, false);
        holder.localRotation = rotation;
        holder.localScale = Vector3.one * local;
        var model = Instance("PSXv2_Player", holder);
        var hitbox = new GameObject("PSXv2 Hitbox").transform;
        hitbox.SetParent(ship, false);
        hitbox.localRotation = rotation;
        hitbox.localScale = Vector3.one * local;
        var markers = new List<Renderer>();
        foreach (var r in model.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.name.Contains("Hitbox")).ToList())
        {
            r.transform.SetParent(hitbox, true);
            r.sharedMaterial = r.name.Contains("Core") ? core : ring;
            r.sortingLayerName = "Bullet";
            r.sortingOrder = r.name.Contains("Core") ? 61 : 60;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            markers.Add(r);
        }
        var parts = model.GetComponentsInChildren<MeshRenderer>(true).ToArray();
        foreach (var r in parts)
        {
            r.sharedMaterial = steel;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
        SetLayer(holder, ship.gameObject.layer);
        SetLayer(hitbox, ship.gameObject.layer);
        visual.Configure(ship.GetComponent<SpriteRenderer>(), holder, parts, rotation, markers.ToArray());
        var effects = ship.GetComponent<PSXShipEffects>();
        if (effects != null)
        {
            // Thruster nozzles between the wing blades and the body (Blender +-0.385, -0.36), in Ship local space,
            // with plumes scaled down to the small ship.
            effects.exhaustOffset = new Vector2(.385f, -.36f) * unitScale / Mathf.Abs(ship.lossyScale.x);
            effects.exhaustScale = .4f;
            EditorUtility.SetDirty(effects);
        }
        EditorUtility.SetDirty(visual);

        // Hexagonal hitbox collider, exactly under the drawn core (replaces the old circle).
        var hexagon = HexCollider(ship);
        var disc = markers.First(m => m.name.Contains("Core")).bounds;
        Vector2 colliderCenter = hexagon.bounds.center;
        float colliderRadius = hexagon.bounds.extents.y;
        var hull = Bounds(parts);
        report.AppendFormat(CultureInfo.InvariantCulture,
            "\"{0}\":{{\"hitboxCenterError\":{1:0.0000},\"hitboxRadius\":{2:0.0000},\"colliderRadius\":{3:0.0000},\"noseUp\":{4},\"shipLength\":{5:0.00},\"partRenderers\":{6}}},",
            Path.GetFileNameWithoutExtension(path), Vector2.Distance((Vector2)disc.center, colliderCenter), disc.extents.x, colliderRadius,
            (hull.max.y - ship.position.y > ship.position.y - hull.min.y).ToString().ToLowerInvariant(), hull.size.y, parts.Length);
        // Miguel removed the hexagon hitbox markers on purpose (2026-10-20): they are only used above to
        // verify the hitbox placement, then deleted. The yellow cockpit glow on the hull marks the hitbox.
        UnityEngine.Object.DestroyImmediate(hitbox.gameObject);
        visual.Configure(ship.GetComponent<SpriteRenderer>(), holder, parts, rotation, new Renderer[0]);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static PolygonCollider2D HexCollider(Transform ship)
    {
        bool trigger = true;
        var circle = ship.GetComponent<CircleCollider2D>();
        if (circle != null)
        {
            trigger = circle.isTrigger;
            UnityEngine.Object.DestroyImmediate(circle);
        }
        var polygon = ship.GetComponent<PolygonCollider2D>();
        if (polygon == null)
            polygon = ship.gameObject.AddComponent<PolygonCollider2D>();
        float scale = Mathf.Abs(ship.lossyScale.x);
        var points = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            float a = Mathf.PI / 2 + i * Mathf.PI / 3;
            points[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * HitboxRadius / scale;
        }
        polygon.offset = HitboxCenter / scale;
        polygon.pathCount = 1;
        polygon.SetPath(0, points);
        polygon.isTrigger = trigger;
        Physics2D.SyncTransforms();
        return polygon;
    }

    // ------------------------------------------------------------------ enemies
    static void InstallEnemy(GameObject root, string modelName, Quaternion rotation, Material steel, StringBuilder report)
    {
        foreach (var name in new[] { "Industrial model", "PSXv2 model" })
        {
            var old = root.transform.Find(name);
            if (old != null)
                UnityEngine.Object.DestroyImmediate(old.gameObject);
        }
        foreach (var sprite in root.GetComponentsInChildren<SpriteRenderer>(true))
            sprite.enabled = false;
        foreach (var particles in root.GetComponentsInChildren<ParticleSystemRenderer>(true))
            particles.enabled = false;
        // PSXObjectVisual wobbles the holder's rotation, so the axis correction lives one level down.
        var holder = new GameObject("PSXv2 model").transform;
        holder.SetParent(root.transform, false);
        var orient = new GameObject("Axis correction").transform;
        orient.SetParent(holder, false);
        orient.localRotation = rotation;
        var model = Instance(modelName, orient);
        var renderers = model.GetComponentsInChildren<MeshRenderer>(true);
        var spriteSource = root.GetComponent<SpriteRenderer>();
        foreach (var r in renderers)
        {
            r.sharedMaterial = steel;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            if (spriteSource != null)
            {
                r.sortingLayerID = spriteSource.sortingLayerID;
                r.sortingOrder = spriteSource.sortingOrder;
            }
        }
        SetLayer(holder, root.layer);
        // Fit to the unchanged gameplay collider, as the previous installer did.
        // Collider sizes are root-local; renderer bounds are world, so divide out the root scale
        // (stage1boss is scaled 0.5, which previously drew the boss at twice its hull size).
        var size = ColliderSize(root);
        var bounds = Bounds(renderers);
        var rootScale = root.transform.lossyScale;
        float fit = Mathf.Min(size.x * Mathf.Abs(rootScale.x) / Mathf.Max(bounds.size.x, .01f), size.y * Mathf.Abs(rootScale.y) / Mathf.Max(bounds.size.y, .01f));
        holder.localScale = Vector3.one * fit;
        var hull = renderers.OrderByDescending(r => r.bounds.size.sqrMagnitude).First();
        var animation = model.GetComponent<Animation>();
        if (animation == null)
            animation = model.AddComponent<Animation>();
        var clip = AssetDatabase.LoadAllAssetsAtPath(Folder + modelName + ".fbx").OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        if (clip != null)
        {
            clip.wrapMode = WrapMode.Loop;
            if (animation.GetClip(clip.name) == null)
                animation.AddClip(clip, clip.name);
            animation.clip = clip;
        }
        animation.playAutomatically = true;
        animation.wrapMode = WrapMode.Loop;
        animation.cullingType = AnimationCullingType.AlwaysAnimate;
        var visual = root.GetComponent<PSXObjectVisual>();
        if (visual == null)
            visual = root.AddComponent<PSXObjectVisual>();
        visual.model = holder;
        visual.modelRenderer = hull;
        visual.modelRenderers = renderers;
        visual.sourceSprite = spriteSource;
        visual.spin = false;
        visual.accent = Color.white;
        foreach (var relay in root.GetComponentsInChildren<PSXVisibilityRelay>(true))
            UnityEngine.Object.DestroyImmediate(relay);
        hull.gameObject.AddComponent<PSXVisibilityRelay>().target = root;
        WireShooters(root, model.transform);
        int tris = renderers.Sum(r => r.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3);
        report.AppendFormat(CultureInfo.InvariantCulture, "{{\"prefab\":\"{0}\",\"model\":\"{1}\",\"triangles\":{2},\"parts\":{3},\"clip\":\"{4}\",\"clipSeconds\":{5:0.00},\"fit\":{6:0.000},\"collider\":\"{7}\"}}",
            root.name, modelName, tris, renderers.Length, clip != null ? clip.name : "", clip != null ? clip.length : 0, fit, size);
    }

    /// <summary>Links model parts to the shooters they represent: aimed shooters drive turrets that
    /// track the player, circular spreads drive parts that spin while firing, and every real shot
    /// sparks from the part (see PSXShooterVisual). Reach is the muzzle distance in model units.</summary>
    static void WireShooters(GameObject root, Transform model)
    {
        foreach (var old in model.GetComponentsInChildren<PSXShooterVisual>(true))
            UnityEngine.Object.DestroyImmediate(old);
        Transform Part(string name) => model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
        Transform[] Shooters(string prefix, float side) => root.GetComponentsInChildren<AShooter>(true)
            .Where(s => s.name.StartsWith(prefix) && Mathf.Sign(s.transform.localPosition.x) == side).Select(s => s.transform).ToArray();
        void Add(string part, PSXShooterVisual.Mode mode, float reach, params Transform[] sources)
        {
            var t = Part(part);
            if (t == null)
                throw new InvalidOperationException(root.name + ": missing part " + part);
            var visual = t.gameObject.AddComponent<PSXShooterVisual>();
            visual.mode = mode;
            visual.reach = reach * Mathf.Abs(t.lossyScale.x);
            visual.sources = sources;
        }
        switch (root.name)
        {
            case "enemy1":
                Add("Dart_Eye", PSXShooterVisual.Mode.Fixed, .1f, root.transform);
                break;
            case "waver":
                Add("Rotor_Hub", PSXShooterVisual.Mode.Fixed, .25f, root.transform);
                break;
            case "Biggie":
                Add("Gunboat_Turret", PSXShooterVisual.Mode.Spin, 1.2f, root.transform);
                break;
            case "stage1boss":
                foreach (var (side, name) in new[] { (1f, "R"), (-1f, "L") })
                {
                    Add("Warden_Drum_" + name, PSXShooterVisual.Mode.Spin, 1.55f, Shooters("Circular", side));
                    Add("Warden_Aim_" + name, PSXShooterVisual.Mode.Aim, 1.25f, Shooters("Focusing", side).Concat(Shooters("DeFocusing", side)).Concat(Shooters("smallShooter", side)).ToArray());
                }
                break;
        }
    }

    static Vector2 ColliderSize(GameObject go)
    {
        var box = go.GetComponent<BoxCollider2D>();
        if (box != null)
            return box.size;
        var circle = go.GetComponent<CircleCollider2D>();
        if (circle != null)
            return Vector2.one * circle.radius * 2;
        var polygon = go.GetComponent<PolygonCollider2D>();
        if (polygon != null)
        {
            var min = Vector2.positiveInfinity;
            var max = Vector2.negativeInfinity;
            for (int path = 0; path < polygon.pathCount; path++)
                foreach (var p in polygon.GetPath(path))
                {
                    min = Vector2.Min(min, p);
                    max = Vector2.Max(max, p);
                }
            return max - min;
        }
        var capsule = go.GetComponent<CapsuleCollider2D>();
        if (capsule != null)
            return capsule.size;
        return Vector2.one;
    }

    static void Edit(string path, Action<GameObject> edit)
    {
        var go = PrefabUtility.LoadPrefabContents(path);
        try
        {
            edit(go);
            PrefabUtility.SaveAsPrefabAsset(go, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(go); }
    }

    // ------------------------------------------------------------------ items
    /// <summary>Bakes each Item_* mesh of PSXv2_Items.fbx into game orientation (upright, facing the
    /// camera, centred) as Assets/Art/PSXv2/Items/*.asset, then swaps them into the pickups, the shop
    /// item (per-kind mesh list), and the four companions, which spin them through PSXObjectVisual.</summary>
    static void InstallItems(Quaternion rotation, Material steel, StringBuilder report)
    {
        var meshes = Bake("PSXv2_Items", "Items", IndustrialMeshFactory.ItemKinds.Select(k => "Item_" + k).ToArray(), rotation);
        string[] pickups = { "life", "mana", "money" };
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            Edit("Assets/Resources/Pickables/" + pickups[i] + ".prefab", go => ItemVisual(go, meshes[index], steel, Vector2.one * .55f));
        }
        Edit("Assets/Resources/Items/Item.prefab", go => { var visual = ItemVisual(go, meshes[4], steel, Vector2.one * .85f); visual.itemMeshes = meshes; });
        string[] companions = { "AMoon", "AngryMoon", "BabyYou", "Twerp" };
        int[] kinds = { 10, 14, 11, 5 };
        for (int i = 0; i < companions.Length; i++)
        {
            int index = i;
            Edit("Assets/Resources/Companions/" + companions[i] + ".prefab", go =>
     {
         var size = ColliderSize(go);
         if (index >= 2)
             size = Vector2.one * (index == 2 ? 1.6f : .65f);   // tiny follower colliders are not a readable size
         ItemVisual(go, meshes[kinds[index]], steel, size);
     });
        }
        report.Append("\"items\":[" + string.Join(",", meshes.Select(m => "{\"kind\":\"" + m.name + "\",\"triangles\":" + m.triangles.Length / 3 + "}")) + "]");
    }

    /// <summary>Bakes named meshes of an FBX into game orientation (axis correction applied, centred)
    /// as Folder/subfolder/*.asset, in the order given.</summary>
    static Mesh[] Bake(string fbx, string subfolder, string[] names, Quaternion rotation)
    {
        Directory.CreateDirectory(Folder + subfolder);
        AssetDatabase.Refresh();
        var temp = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + fbx + ".fbx"));
        temp.transform.position = Vector3.zero;
        try
        {
            return names.Select(name =>
            {
                var source = temp.GetComponentsInChildren<MeshFilter>(true).Single(m => m.name == name);
                var matrix = Matrix4x4.Rotate(rotation) * source.transform.localToWorldMatrix;
                var mesh = UnityEngine.Object.Instantiate(source.sharedMesh);
                var vertices = mesh.vertices.Select(v => matrix.MultiplyPoint3x4(v)).ToArray();
                var normals = mesh.normals.Select(n => matrix.MultiplyVector(n).normalized).ToArray();
                var center = new Bounds(vertices[0], Vector3.zero);
                foreach (var v in vertices)
                    center.Encapsulate(v);
                for (int k = 0; k < vertices.Length; k++)
                    vertices[k] -= center.center;
                mesh.vertices = vertices;
                mesh.normals = normals;
                mesh.RecalculateBounds();
                mesh.name = "PSXv2 " + name.Substring(name.IndexOf('_') + 1);
                return SaveMesh(mesh, Folder + subfolder + "/" + name.Substring(name.IndexOf('_') + 1) + ".asset");
            }).ToArray();
        }
        finally { UnityEngine.Object.DestroyImmediate(temp); }
    }

    // ------------------------------------------------------------------ level 1 field
    /// <summary>Level 1 only: a grass field (PSXGrassField: tiled grass, model puddles/bushes/trees,
    /// sprite clouds) replaces the industrial shaft. The
    /// PSXIndustrialField object stays because it also sets up the PSX presentation; clearing its
    /// steel material makes it skip building the shaft geometry.</summary>
    static void InstallField(Quaternion rotation, Material steel, StringBuilder report)
    {
        string[] Names(string kind, int count) => Enumerable.Range(0, count).Select(i => "Field_" + kind + i).ToArray();
        var puddles = Bake("PSXv2_Field", "Field", Names("Puddle", 5), rotation);
        var rocks = Bake("PSXv2_Field", "Field", Names("Rock", 3), rotation);
        var bushes = Bake("PSXv2_Field", "Field", Names("Bush", 2), rotation);
        var trees = Bake("PSXv2_Field", "Field", Names("Tree", 3), rotation);
        var grass = Mat(Folder + "Field/PSXv2 Grass.mat", "Shootemup/PSXv2 Lit");
        grass.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "Field/Grass_256.png");
        grass.SetFloat("_GlowRow", 0);
        grass.SetFloat("_Dither", 0);
        grass.SetFloat("_VertexSnap", 1);
        var cloudMaterial = Mat(Folder + "Field/PSXv2 Cloud.mat", "Shootemup/PSXv2 Sprite");
        cloudMaterial.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "Field/Clouds_64x128.png");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/L1.unity", OpenSceneMode.Single);
        var roots = scene.GetRootGameObjects();
        foreach (var industrial in roots.SelectMany(r => r.GetComponentsInChildren<PSXIndustrialField>(true)))
        {
            industrial.steelMaterial = null;
            EditorUtility.SetDirty(industrial);
        }
        foreach (var old in roots.Where(r => r.name == "PSX Grass Field"))
            UnityEngine.Object.DestroyImmediate(old);
        var field = new GameObject("PSX Grass Field").AddComponent<PSXGrassField>();
        field.puddles = puddles;
        field.rocks = rocks;
        field.bushes = bushes;
        field.trees = trees;
        field.material = steel;
        field.groundMaterial = grass;
        field.cloudMaterial = cloudMaterial;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        report.Append("\"field\":{\"pieces\":" + (puddles.Length + rocks.Length + bushes.Length + trees.Length) + "},");
    }

    static Mesh SaveMesh(Mesh mesh, string path)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
        // Update through Mesh APIs so renderers pick up the new buffers.
        existing.Clear();
        existing.name = mesh.name;
        existing.SetVertices(mesh.vertices);
        existing.SetNormals(mesh.normals);
        existing.SetUVs(0, mesh.uv);
        existing.SetColors(mesh.colors);
        existing.SetTriangles(mesh.triangles, 0);
        existing.RecalculateBounds();
        UnityEngine.Object.DestroyImmediate(mesh);
        EditorUtility.SetDirty(existing);
        return existing;
    }

    static PSXObjectVisual ItemVisual(GameObject root, Mesh mesh, Material material, Vector2 size)
    {
        var visual = root.GetComponent<PSXObjectVisual>();
        var model = root.transform.Find("Industrial model");
        if (visual == null || model == null)
            throw new InvalidOperationException(root.name + " has no industrial item visual to replace");
        model.GetComponent<MeshFilter>().sharedMesh = mesh;
        model.GetComponent<MeshRenderer>().sharedMaterial = material;
        float scale = Mathf.Min(size.x / Mathf.Max(mesh.bounds.size.x, .01f), size.y / Mathf.Max(mesh.bounds.size.y, .01f));
        model.localScale = Vector3.one * scale;
        return visual;
    }

    // ------------------------------------------------------------------ capture
    static void Stage(string resource, Vector3 position)
    {
        var go = UnityEngine.Object.Instantiate(Resources.Load<GameObject>(resource), position, Quaternion.identity);
        // Inert except for its shooters, which fire so the turret visuals can be checked.
        foreach (var behaviour in go.GetComponentsInChildren<MonoBehaviour>(true))
            if (!(behaviour is PSXObjectVisual) && !(behaviour is PSXVisibilityRelay) && !(behaviour is PSXShooterVisual) && !(behaviour is AShooter))
                behaviour.enabled = false;
        foreach (var shooter in go.GetComponentsInChildren<AShooter>(true))
        {
            if (shooter is Multishooter || shooter is Multiphase)
            {
                shooter.enabled = false;
                continue;
            }
            shooter.activation = AShooter.ShootTrigger.onTrigger;
            shooter.Trigger();
        }
        foreach (var collider in go.GetComponentsInChildren<Collider2D>(true))
            collider.enabled = false;
        foreach (var body in go.GetComponentsInChildren<Rigidbody2D>(true))
            body.simulated = false;
    }

    static void Capture(int stage)
    {
        if (stage == 1)
        {
            if (!EditorApplication.isPlaying)
                return;
            double now = EditorApplication.timeSinceStartup;
            float start = SessionState.GetFloat(StartKey, 0);
            if (start <= 0)
            {
                SessionState.SetFloat(StartKey, (float)now);
                return;
            }
            int shot = SessionState.GetInt("PSXv2CaptureShot", 0);
            if (now - start >= 2.5f && !SessionState.GetBool("PSXv2Staged", false))
            {
                // Inert boss + gunboat to check in-game scale and animation, plus test explosions.
                SessionState.SetBool("PSXv2Staged", true);
                var cam = UnityEngine.Camera.main.transform.position;
                Stage("Enemy/Bosses/stage1boss", new Vector3(cam.x, cam.y + 4.2f, 0));
                Stage("Enemy/Biggie", new Vector3(cam.x + 2.4f, cam.y - .5f, 0));
                PSXImpactEffects.Explosion(new Vector3(cam.x - 2.4f, cam.y + .6f, 0), 1f);
                PSXImpactEffects.Explosion(new Vector3(cam.x - 1.2f, cam.y - 2.6f, 0), 4f);
                PSXFireballs.Shockwave(new Vector3(cam.x, cam.y - 5, 0), 5, .45f, new Color(.55f, .8f, 1f));
            }
            if (now - start < 6 + shot * .2f)
                return;
            var presentation = UnityEngine.Camera.main != null ? UnityEngine.Camera.main.GetComponent<PSXPresentation>() : null;
            if (presentation != null && presentation.SceneTexture != null)
            {
                var scene = presentation.SceneTexture;
                var target = RenderTexture.GetTemporary(scene.width, scene.height, 0, RenderTextureFormat.ARGB32);
                if (presentation.filterEnabled && presentation.CompositeMaterial != null)
                    Graphics.Blit(scene, target, presentation.CompositeMaterial);
                else
                    Graphics.Blit(scene, target);
                var active = RenderTexture.active;
                RenderTexture.active = target;
                var image = new Texture2D(scene.width, scene.height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, scene.width, scene.height), 0, 0);
                image.Apply();
                RenderTexture.active = active;
                RenderTexture.ReleaseTemporary(target);
                File.WriteAllBytes(CaptureFolder + "/frame_" + shot + ".png", image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);
                if (shot == 0)
                    File.WriteAllText(CaptureFolder + "/capture.json", "{\"width\":" + scene.width + ",\"height\":" + scene.height + ",\"filterEnabled\":" + presentation.filterEnabled.ToString().ToLowerInvariant() +
             ",\"presentations\":" + UnityEngine.Object.FindObjectsByType<PSXPresentation>().Length + ",\"renderHeight\":" + presentation.renderHeight + ",\"explosions\":" + PSXImpactEffects.ExplosionCount + ",\"bursts\":" + PSXImpactEffects.BurstCount + "}");
            }
            shot++;
            SessionState.SetInt("PSXv2CaptureShot", shot);
            if (shot >= 4)
            {
                SessionState.SetInt("PSXv2CaptureShot", 0);
                SessionState.SetBool("PSXv2Staged", false);
                SessionState.SetInt(StageKey, 2);
                EditorApplication.isPlaying = false;
            }
            return;
        }
        if (stage == 2 && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SessionState.SetInt(StageKey, 0);
            EditorSceneManager.OpenScene("Assets/Scenes/L1.unity");
            File.WriteAllText(CaptureFolder + "/done.txt", DateTime.Now.ToString("s"));
        }
    }
}
