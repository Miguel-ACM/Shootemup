using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class InstallPSXVisualPass
{
    const string Folder="Assets/Art/PSXPass/";
    const string Request="ArtSource/PSXPreview/install.request";

    static InstallPSXVisualPass() { EditorApplication.delayCall+=CheckRequest; }
    static void CheckRequest()
    {
        if(!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode)return;
        try { Install();File.Delete(Request); }
        catch(Exception error) { File.WriteAllText("ArtSource/PSXPreview/InstallError.txt",error.ToString());Debug.LogException(error); }
    }

    [MenuItem("Tools/PSX Player/Install industrial visual pass")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode first.");
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        var meshes=IndustrialMeshFactory.CreateFieldMeshes();
        for(int i=0;i<meshes.Length;i++)meshes[i]=Save(meshes[i],Folder+"IndustrialPart_"+i+".asset");
        var bulletMesh=Save(IndustrialMeshFactory.CreateProjectile(),Folder+"ArmourPiercingTracer.asset");
        var flameMesh=Save(IndustrialMeshFactory.CreateFlame(),Folder+"ExhaustPlume.asset");
        var atlas=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/PlayerShip/GR07_Atlas.png");
        var steel=Material("Industrial salvage", "Shootemup/PSX Industrial Ship");steel.mainTexture=atlas;steel.SetFloat("_VertexSnap",.45f);
        var bullet=Material("Autocannon steel", "Shootemup/PSX Industrial Ship");bullet.mainTexture=atlas;bullet.SetFloat("_VertexSnap",0);
        var exhaust=Material("Amber exhaust", "Shootemup/PSX Exhaust");exhaust.SetColor("_Tint",Color.white);
        var composite=Material("PSX screen composite", "Shootemup/PSX Composite");

        const string bulletPath="Assets/Resources/Bullets/PlayerBullet.prefab";
        var projectile=PrefabUtility.LoadPrefabContents(bulletPath);
        try
        {
            var sprite=projectile.GetComponent<SpriteRenderer>();
            if(sprite!=null)UnityEngine.Object.DestroyImmediate(sprite);
            var filter=projectile.GetComponent<MeshFilter>();if(filter==null)filter=projectile.AddComponent<MeshFilter>();filter.sharedMesh=bulletMesh;
            var renderer=projectile.GetComponent<MeshRenderer>();if(renderer==null)renderer=projectile.AddComponent<MeshRenderer>();renderer.sharedMaterial=bullet;
            renderer.sortingLayerName="Bullet";renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            var effects=projectile.GetComponent<PSXProjectileEffects>();if(effects==null)effects=projectile.AddComponent<PSXProjectileEffects>();effects.tracerMaterial=exhaust;
            PrefabUtility.SaveAsPrefabAsset(projectile,bulletPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(projectile); }

        var current=SceneManager.GetActiveScene();
        var scene=SceneManager.GetSceneByPath("Assets/Scenes/L1.unity");
        if(!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/L1.unity",OpenSceneMode.Single);
        SceneManager.SetActiveScene(scene);
        var roots=scene.GetRootGameObjects();
        var player=roots.SelectMany(r=>r.GetComponentsInChildren<Shipcontroller>(true)).Single();
        var shipEffects=player.GetComponent<PSXShipEffects>();if(shipEffects==null)shipEffects=player.gameObject.AddComponent<PSXShipEffects>();shipEffects.flameMesh=flameMesh;shipEffects.exhaustMaterial=exhaust;
        foreach(var background in roots.SelectMany(r=>r.GetComponentsInChildren<ScrollingBg>(true)))background.gameObject.SetActive(false);
        foreach(var clouds in roots.SelectMany(r=>r.GetComponentsInChildren<CloudSpawner>(true)))clouds.enabled=false;
        foreach(var root in roots.Where(r=>r.name=="PSX Industrial Field"))UnityEngine.Object.DestroyImmediate(root);
        var fieldObject=new GameObject("PSX Industrial Field");
        var field=fieldObject.AddComponent<PSXIndustrialField>();field.debrisMeshes=meshes;field.steelMaterial=steel;field.compositeMaterial=composite;
        EditorUtility.SetDirty(shipEffects);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);

        // Keep a separate demonstration scene; the normal L1 waves and UI are retained.
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/PSX_Showcase.unity",true);
        var showcase=EditorSceneManager.OpenScene("Assets/Scenes/PSX_Showcase.unity",OpenSceneMode.Single);
        foreach(var root in showcase.GetRootGameObjects())
        {
            if(root.name=="UI" || root.GetComponent<WaveOrchestrer>()!=null)root.SetActive(false);
        }
        var previewObject=new GameObject("PSX Showcase Playback");var preview=previewObject.AddComponent<PSXShowcase>();
        preview.hudFont=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/prstartk.ttf");
        foreach(var audio in showcase.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<AudioSource>(true)))audio.volume=Mathf.Min(audio.volume,.08f);
        EditorSceneManager.MarkSceneDirty(showcase);EditorSceneManager.SaveScene(showcase);AssetDatabase.SaveAssets();
        File.WriteAllText("ArtSource/PSXPreview/InstallReport.json","{\"success\":true,\"projectileTriangles\":28,\"fieldMeshTriangles\":[12,36,36,36,16,36],\"previewScene\":\"Assets/Scenes/PSX_Showcase.unity\",\"runtimeScene\":\"Assets/Scenes/L1.unity\"}");
        Debug.Log("PSX visual pass installed: tracers, animated twin exhaust, dithered low-res output and recycled industrial meshes.");
        EditorApplication.delayCall+=()=> { EditorApplication.isPlaying=true; };
    }

    static T Save<T>(T value,string path) where T:UnityEngine.Object
    {
        var existing=AssetDatabase.LoadAssetAtPath<T>(path);
        if(existing!=null){EditorUtility.CopySerialized(value,existing);UnityEngine.Object.DestroyImmediate(value);EditorUtility.SetDirty(existing);return existing;}
        AssetDatabase.CreateAsset(value,path);return value;
    }

    static Material Material(string name,string shaderName)
    {
        string path=Folder+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        var shader=Shader.Find(shaderName);if(shader==null)throw new InvalidOperationException("Missing shader: "+shaderName);
        if(material==null){material=new Material(shader){name=name};AssetDatabase.CreateAsset(material,path);}
        material.shader=shader;EditorUtility.SetDirty(material);return material;
    }
}


