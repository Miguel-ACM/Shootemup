using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Repeatable import/install action for the original GR-07 art asset.</summary>
[InitializeOnLoad]
public static class InstallPSXPlayerShip
{
    const string Folder = "Assets/Art/PlayerShip/";
    const string Request = "ArtSource/PlayerShip/install.request";
    const string Report = "ArtSource/PlayerShip/UnityImportReport.json";

    static InstallPSXPlayerShip()
    {
        EditorApplication.delayCall += HandleRequest;
    }

    static void HandleRequest()
    {
        if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        try
        {
            Install();
            File.Delete(Request);
        }
        catch (Exception exception)
        {
            File.WriteAllText(Report, JsonUtility.ToJson(new ImportReport { error = exception.ToString() }, true));
            Debug.LogException(exception);
        }
    }

    [MenuItem("Tools/PSX Player/Install GR-07 Ship")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play mode before installing the ship.");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var importer = (ModelImporter)AssetImporter.GetAtPath(Folder + "GR07_Interceptor.fbx");
        importer.globalScale = 1f;
        importer.useFileScale = true;
        importer.importCameras = false;
        importer.importLights = false;
        importer.importAnimation = false;
        importer.importNormals = ModelImporterNormals.Import;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.meshCompression = ModelImporterMeshCompression.Off;
        importer.SaveAndReimport();

        var textureImporter = (TextureImporter)AssetImporter.GetAtPath(Folder + "GR07_Atlas.png");
        textureImporter.textureType = TextureImporterType.Default;
        textureImporter.filterMode = FilterMode.Point;
        textureImporter.wrapMode = TextureWrapMode.Clamp;
        textureImporter.mipmapEnabled = false;
        textureImporter.maxTextureSize = 256;
        textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
        textureImporter.SaveAndReimport();

        var shader = Shader.Find("Shootemup/PSX Industrial Ship");
        if (shader == null || !shader.isSupported)
            throw new InvalidOperationException("PSX ship shader is unavailable.");
        var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "GR07_PixelSteel.mat");
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, Folder + "GR07_PixelSteel.mat");
        }
        material.shader = shader;
        material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "GR07_Atlas.png");
        material.SetColor("_Tint", Color.white);
        EditorUtility.SetDirty(material);

        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "GR07_Interceptor.fbx");
        var mesh = fbx.GetComponentInChildren<MeshFilter>().sharedMesh;
        int triangles = (int)(mesh.GetIndexCount(0) / 3);
        if (triangles >= 300 || mesh.uv.Length != mesh.vertexCount)
            throw new InvalidOperationException("Ship does not meet its triangle/UV budget.");
        var root = new GameObject("GR-07 Player Ship Visual");
        var meshObject = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
        meshObject.name = "GR07 Mesh";
        meshObject.transform.SetParent(root.transform, false);
        // Applied at the visual root so gameplay transforms and hitboxes stay intact.
        root.transform.localRotation = Quaternion.Euler(-75f, 0f, 0f) * Quaternion.Euler(0f, 180f, 0f);
        var renderer = meshObject.GetComponentInChildren<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingLayerName = "Player";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, Folder + "GR07_PlayerVisual.prefab");
        UnityEngine.Object.DestroyImmediate(root);

        var scene = SceneManager.GetSceneByPath("Assets/Scenes/L1.unity");
        bool openedHere = !scene.isLoaded;
        if (openedHere)
            scene = EditorSceneManager.OpenScene("Assets/Scenes/L1.unity", OpenSceneMode.Additive);
        var player = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Shipcontroller>(true)).Single();
        // Unity names a saved prefab root after its filename. Remove either instance
        // spelling so repeating the import cannot stack a second ship on the player.
        for (int i = player.transform.childCount - 1; i >= 0; i--)
        {
            var child = player.transform.GetChild(i);
            if (child.name == "GR-07 Player Ship Visual" || child.name == "GR07_PlayerVisual")
                UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, player.transform);
        visual.name = "GR-07 Player Ship Visual";
        visual.transform.localPosition = Vector3.zero;
        // The current player root has scale 2; preserve its familiar on-screen footprint.
        visual.transform.localScale = Vector3.one * 0.21f;
        visual.layer = player.gameObject.layer;
        foreach (var child in visual.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = player.gameObject.layer;
        var binding = player.GetComponent<PSXShipVisual>() ?? player.gameObject.AddComponent<PSXShipVisual>();
        var sprite = player.GetComponent<SpriteRenderer>();
        binding.Configure(sprite, visual.transform, visual.GetComponentInChildren<MeshRenderer>());
        EditorUtility.SetDirty(binding);
        EditorUtility.SetDirty(sprite);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        CapturePreview(player);
        File.WriteAllText(Report, JsonUtility.ToJson(new ImportReport
        {
            triangles = triangles,
            vertices = mesh.vertexCount,
            uvCount = mesh.uv.Length,
            textureWidth = material.mainTexture.width,
            textureHeight = material.mainTexture.height,
            shader = shader.name,
            scene = scene.path,
            player = player.name,
            colliderPreserved = player.GetComponent<CircleCollider2D>() != null,
            rigidbodyPreserved = player.GetComponent<Rigidbody2D>() != null,
            materialCount = 1,
            visualInstances = player.GetComponentsInChildren<MeshRenderer>().Length,
            success = true
        }, true));
        if (openedHere)
            EditorSceneManager.CloseScene(scene, true);
        Debug.Log("GR-07 installed: " + triangles + " triangles, atlas UVs, 256px point-filtered texture, original 2D player controls.");
    }

    static void CapturePreview(Shipcontroller player)
    {
        var cameraObject = new GameObject("GR07 temporary preview camera") { hideFlags = HideFlags.HideAndDontSave };
        var camera = cameraObject.AddComponent<UnityEngine.Camera>();
        camera.transform.position = player.transform.position + Vector3.back * 10f;
        camera.orthographic = true;
        camera.orthographicSize = 0.82f;
        camera.aspect = 1f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.075f, 0.085f, 0.095f, 1f);
        camera.cullingMask = 1 << player.gameObject.layer;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 30f;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        var target = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
        var previous = RenderTexture.active;
        var image = new Texture2D(512, 512, TextureFormat.RGB24, false);
        try
        {
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
            image.Apply();
            File.WriteAllBytes("ArtSource/PlayerShip/GR07_UnityPreview.png", image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }

    [Serializable]
    sealed class ImportReport
    {
        public bool success;
        public string error;
        public int triangles, vertices, uvCount, textureWidth, textureHeight, materialCount, visualInstances;
        public string shader, scene, player;
        public bool colliderPreserved, rigidbodyPreserved;
    }
}
