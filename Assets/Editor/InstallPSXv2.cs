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
    const string Folder="Assets/Art/PSXv2/";
    const string Request="ArtSource/PSXv2/install.request";
    const string CaptureRequest="ArtSource/PSXv2/capture.request";
    const string ReportPath="ArtSource/PSXv2/InstallReport.json";
    const string CaptureFolder="ArtSource/PSXv2/UnityCapture";
    const string StageKey="PSXv2CaptureStage",StartKey="PSXv2CaptureStart";
    static readonly string[] Scenes={"Assets/Scenes/L1.unity","Assets/Scenes/PSX_Showcase.unity"};
    static readonly (string prefab,string model)[] Enemies=
    {
        ("enemy1","PSXv2_Enemy_0_Dart"),("waver","PSXv2_Enemy_1_Rotor"),
        ("Biggie","PSXv2_Enemy_2_Gunboat"),("Bosses/stage1boss","PSXv2_Enemy_3_Warden"),
    };
    // Blender-space facts from build_ships.py: hitbox disc radius and centre (= CircleCollider2D in world units).
    const float HitboxRadius=.115f;
    static readonly Vector2 HitboxCenter=new Vector2(0,.16f);
    static bool busy;

    static InstallPSXv2(){EditorApplication.update+=Tick;}

    static void Tick()
    {
        if(busy || EditorApplication.isCompiling || EditorApplication.isUpdating)return;
        int stage=SessionState.GetInt(StageKey,0);
        if(stage>0){busy=true;try{Capture(stage);}catch(Exception e){Fail(e);SessionState.SetInt(StageKey,0);}finally{busy=false;}return;}
        if(File.Exists(CaptureRequest) && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            File.Delete(CaptureRequest);Directory.CreateDirectory(CaptureFolder);
            EditorSceneManager.OpenScene("Assets/Scenes/PSX_Showcase.unity");
            SessionState.SetInt(StageKey,1);SessionState.SetFloat(StartKey,0);EditorApplication.isPlaying=true;return;
        }
        if(!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode)return;
        busy=true;
        try{File.Delete(Request);Install();}
        catch(Exception e){Fail(e);}
        finally{busy=false;}
    }

    static void Fail(Exception e){File.WriteAllText("ArtSource/PSXv2/InstallError.txt",e.ToString());Debug.LogException(e);}

    [MenuItem("Tools/PSX Player/Install PSXv2 Blender fleet")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode first.");
        if(File.Exists("ArtSource/PSXv2/InstallError.txt"))File.Delete("ArtSource/PSXv2/InstallError.txt");
        var report=new StringBuilder("{");
        ConfigureTextures();
        ConfigureModels(report);
        var steel=Mat(Folder+"PSXv2 Steel.mat","Shootemup/PSXv2 Lit");
        steel.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"PSXv2_Atlas.png");
        steel.SetFloat("_VertexSnap",1);steel.SetFloat("_Affine",1);steel.SetFloat("_Dither",0);
        var core=Mat(Folder+"PSXv2 Hitbox Core.mat","Shootemup/PSXv2 Hitbox");core.SetColor("_Color",Color.white);
        var ring=Mat(Folder+"PSXv2 Hitbox Ring.mat","Shootemup/PSXv2 Hitbox");ring.SetColor("_Color",new Color(1,.12f,.35f));
        Directory.CreateDirectory("Assets/Resources/Effects");
        Flipbook("PSXv2Spark","Spark_4x16",true);Flipbook("PSXv2Explosion","Explosion_8x32",false);Flipbook("PSXv2ExplosionBig","ExplosionBig_8x64",false);
        // Full PSX presentation by default: the shared composite now dithers visibly.
        var screen=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/PSXPass/PSX screen composite.mat");
        if(screen!=null){screen.SetFloat("_Dither",.55f);screen.SetFloat("_Vignette",.12f);EditorUtility.SetDirty(screen);}

        Orientation(out var rotation,out float scale,report);
        foreach(var path in Scenes)InstallPlayer(path,rotation,scale,steel,core,ring,report);
        report.Append("\"enemies\":[");
        for(int i=0;i<Enemies.Length;i++)
        {
            var (prefab,model)=Enemies[i];if(i>0)report.Append(',');
            Edit("Assets/Resources/Enemy/"+prefab+".prefab",go=>InstallEnemy(go,model,rotation,steel,report));
        }
        report.Append("]}");
        AssetDatabase.SaveAssets();
        File.WriteAllText(ReportPath,report.ToString());Debug.Log("PSXv2 installed: "+report);
        // Play the showcase (autopilot + sample fleet) and capture a few frames.
        Directory.CreateDirectory(CaptureFolder);
        EditorSceneManager.OpenScene("Assets/Scenes/PSX_Showcase.unity");
        SessionState.SetInt(StageKey,1);SessionState.SetFloat(StartKey,0);
        EditorApplication.isPlaying=true;
    }

    // ------------------------------------------------------------------ importers
    static void ConfigureTextures()
    {
        var paths=new List<string>{Folder+"PSXv2_Atlas.png"};
        paths.AddRange(Directory.GetFiles(Folder+"FX","*.png").Select(p=>p.Replace('\\','/')));
        foreach(var path in paths)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Default;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;
            importer.npotScale=TextureImporterNPOTScale.None;importer.alphaIsTransparency=path.Contains("/FX/");
            importer.SaveAndReimport();
        }
    }

    static void ConfigureModels(StringBuilder report)
    {
        foreach(var name in new[]{"PSXv2_Player"}.Concat(Enemies.Select(e=>e.model)))
        {
            var importer=(ModelImporter)AssetImporter.GetAtPath(Folder+name+".fbx");
            importer.globalScale=1;importer.useFileScale=true;importer.bakeAxisConversion=false;
            importer.importCameras=false;importer.importLights=false;importer.importVisibility=false;
            importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.None;
            importer.weldVertices=false;importer.meshCompression=ModelImporterMeshCompression.Off;
            importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.importBlendShapes=false;
            bool animated=name!="PSXv2_Player";
            importer.importAnimation=animated;
            importer.animationType=animated ? ModelImporterAnimationType.Legacy : ModelImporterAnimationType.None;
            importer.SaveAndReimport();
            if(animated)
            {
                var clips=importer.defaultClipAnimations;
                foreach(var clip in clips){clip.loopTime=true;clip.wrapMode=WrapMode.Loop;}
                importer.clipAnimations=clips;importer.SaveAndReimport();
            }
        }
    }

    static Material Mat(string path,string shader)
    {
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        var s=Shader.Find(shader);if(s==null)throw new InvalidOperationException("Missing shader "+shader);
        if(material==null){material=new Material(s);AssetDatabase.CreateAsset(material,path);}
        material.shader=s;EditorUtility.SetDirty(material);return material;
    }

    static void Flipbook(string name,string texture,bool additive)
    {
        var material=Mat("Assets/Resources/Effects/"+name+".mat","Shootemup/PSXv2 Flipbook");
        material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"FX/"+texture+".png");
        material.SetFloat("_Additive",additive ? 1 : 0);
    }

    // ------------------------------------------------------------------ orientation
    /// <summary>Finds where Blender's +Y (nose) and +Z (toward camera) land after FBX import, using
    /// the player's asymmetric extents, and the unit scale from the hitbox disc's known width.</summary>
    static void Orientation(out Quaternion rotation,out float scale,StringBuilder report)
    {
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"PSXv2_Player.fbx");
        var temp=UnityEngine.Object.Instantiate(asset);
        try
        {
            temp.transform.position=Vector3.zero;
            var renderers=temp.GetComponentsInChildren<MeshRenderer>();
            var hull=Bounds(renderers.Where(r=>!r.name.Contains("Hitbox")));
            var disc=renderers.Single(r=>r.name.Contains("HitboxCore")).bounds;
            int zAxis=Enumerable.Range(0,3).OrderBy(i=>hull.size[i]).First();
            int yAxis=Enumerable.Range(0,3).Where(i=>i!=zAxis).OrderByDescending(i=>Mathf.Abs(hull.center[i])).First();
            int xAxis=3-zAxis-yAxis;
            Vector3 up=Vector3.zero,toCamera=Vector3.zero;
            up[yAxis]=Mathf.Sign(hull.center[yAxis]);toCamera[zAxis]=Mathf.Sign(hull.center[zAxis]);
            // Unity: nose +Y, top toward the camera which looks along +Z, i.e. -Z.
            rotation=Quaternion.LookRotation(Vector3.back,Vector3.up)*Quaternion.Inverse(Quaternion.LookRotation(toCamera,up));
            scale=2*HitboxRadius/disc.size[xAxis];
            report.AppendFormat(CultureInfo.InvariantCulture,"\"axes\":{{\"x\":{0},\"y\":{1},\"z\":{2},\"ySign\":{3},\"zSign\":{4}}},\"unitScale\":{5},\"rotation\":\"{6}\",",
                xAxis,yAxis,zAxis,up[yAxis],toCamera[zAxis],scale,rotation.eulerAngles);
        }
        finally{UnityEngine.Object.DestroyImmediate(temp);}
    }

    static Bounds Bounds(IEnumerable<Renderer> renderers)
    {
        var list=renderers.ToList();var b=list[0].bounds;foreach(var r in list.Skip(1))b.Encapsulate(r.bounds);return b;
    }

    static GameObject Instance(string model,Transform parent)
    {
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+model+".fbx");
        var go=(GameObject)PrefabUtility.InstantiatePrefab(asset,parent);
        PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        return go;
    }

    static void SetLayer(Transform t,int layer){t.gameObject.layer=layer;foreach(Transform c in t)SetLayer(c,layer);}

    // ------------------------------------------------------------------ player
    static void InstallPlayer(string path,Quaternion rotation,float unitScale,Material steel,Material core,Material ring,StringBuilder report)
    {
        var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
        var visual=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<PSXShipVisual>(true)).Single();
        var ship=visual.transform;
        foreach(var name in new[]{"PSXv2 Player Visual","PSXv2 Hitbox"}){var old=ship.Find(name);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);}
        var serialized=new SerializedObject(visual);
        var previous=serialized.FindProperty("model").objectReferenceValue as Transform;
        if(previous!=null)previous.gameObject.SetActive(false);   // old GR-07 visual kept, disabled

        float local=unitScale/Mathf.Abs(ship.lossyScale.x);
        var holder=new GameObject("PSXv2 Player Visual").transform;holder.SetParent(ship,false);
        holder.localRotation=rotation;holder.localScale=Vector3.one*local;
        var model=Instance("PSXv2_Player",holder);
        var hitbox=new GameObject("PSXv2 Hitbox").transform;hitbox.SetParent(ship,false);
        hitbox.localRotation=rotation;hitbox.localScale=Vector3.one*local;
        var markers=new List<Renderer>();
        foreach(var r in model.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name.Contains("Hitbox")).ToList())
        {
            r.transform.SetParent(hitbox,true);
            r.sharedMaterial=r.name.Contains("Core") ? core : ring;
            r.sortingLayerName="Bullet";r.sortingOrder=r.name.Contains("Core") ? 61 : 60;
            r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;markers.Add(r);
        }
        var parts=model.GetComponentsInChildren<MeshRenderer>(true).ToArray();
        foreach(var r in parts){r.sharedMaterial=steel;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;}
        SetLayer(holder,ship.gameObject.layer);SetLayer(hitbox,ship.gameObject.layer);
        visual.Configure(ship.GetComponent<SpriteRenderer>(),holder,parts,rotation,markers.ToArray());
        var effects=ship.GetComponent<PSXShipEffects>();
        if(effects!=null)
        {
            // Burners at Blender (+-0.26, -0.61), expressed in Ship local space.
            effects.exhaustOffset=new Vector2(.26f,-.61f)*unitScale/Mathf.Abs(ship.lossyScale.x);
            EditorUtility.SetDirty(effects);
        }
        EditorUtility.SetDirty(visual);

        // Verify the drawn hitbox against the collider.
        var collider=ship.GetComponent<CircleCollider2D>();
        var disc=markers.First(m=>m.name.Contains("Core")).bounds;
        Vector2 colliderCenter=ship.TransformPoint(collider.offset);
        float colliderRadius=collider.radius*Mathf.Abs(ship.lossyScale.x);
        var hull=Bounds(parts);
        report.AppendFormat(CultureInfo.InvariantCulture,
            "\"{0}\":{{\"hitboxCenterError\":{1:0.0000},\"hitboxRadius\":{2:0.0000},\"colliderRadius\":{3:0.0000},\"noseUp\":{4},\"shipLength\":{5:0.00},\"partRenderers\":{6}}},",
            Path.GetFileNameWithoutExtension(path),Vector2.Distance((Vector2)disc.center,colliderCenter),disc.extents.x,colliderRadius,
            (hull.max.y-ship.position.y>ship.position.y-hull.min.y).ToString().ToLowerInvariant(),hull.size.y,parts.Length);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }

    // ------------------------------------------------------------------ enemies
    static void InstallEnemy(GameObject root,string modelName,Quaternion rotation,Material steel,StringBuilder report)
    {
        foreach(var name in new[]{"Industrial model","PSXv2 model"}){var old=root.transform.Find(name);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);}
        foreach(var sprite in root.GetComponentsInChildren<SpriteRenderer>(true))sprite.enabled=false;
        foreach(var particles in root.GetComponentsInChildren<ParticleSystemRenderer>(true))particles.enabled=false;
        // PSXObjectVisual wobbles the holder's rotation, so the axis correction lives one level down.
        var holder=new GameObject("PSXv2 model").transform;holder.SetParent(root.transform,false);
        var orient=new GameObject("Axis correction").transform;orient.SetParent(holder,false);orient.localRotation=rotation;
        var model=Instance(modelName,orient);
        var renderers=model.GetComponentsInChildren<MeshRenderer>(true);
        var spriteSource=root.GetComponent<SpriteRenderer>();
        foreach(var r in renderers)
        {
            r.sharedMaterial=steel;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
            if(spriteSource!=null){r.sortingLayerID=spriteSource.sortingLayerID;r.sortingOrder=spriteSource.sortingOrder;}
        }
        SetLayer(holder,root.layer);
        // Fit to the unchanged gameplay collider, as the previous installer did.
        var size=ColliderSize(root);var bounds=Bounds(renderers);
        float fit=Mathf.Min(size.x/Mathf.Max(bounds.size.x,.01f),size.y/Mathf.Max(bounds.size.y,.01f));
        holder.localScale=Vector3.one*fit;
        var hull=renderers.OrderByDescending(r=>r.bounds.size.sqrMagnitude).First();
        var animation=model.GetComponent<Animation>();if(animation==null)animation=model.AddComponent<Animation>();
        var clip=AssetDatabase.LoadAllAssetsAtPath(Folder+modelName+".fbx").OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__preview__"));
        if(clip!=null){clip.wrapMode=WrapMode.Loop;if(animation.GetClip(clip.name)==null)animation.AddClip(clip,clip.name);animation.clip=clip;}
        animation.playAutomatically=true;animation.wrapMode=WrapMode.Loop;animation.cullingType=AnimationCullingType.AlwaysAnimate;
        var visual=root.GetComponent<PSXObjectVisual>();if(visual==null)visual=root.AddComponent<PSXObjectVisual>();
        visual.model=holder;visual.modelRenderer=hull;visual.modelRenderers=renderers;visual.sourceSprite=spriteSource;visual.spin=false;visual.accent=Color.white;
        foreach(var relay in root.GetComponentsInChildren<PSXVisibilityRelay>(true))UnityEngine.Object.DestroyImmediate(relay);
        hull.gameObject.AddComponent<PSXVisibilityRelay>().target=root;
        int tris=renderers.Sum(r=>r.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3);
        report.AppendFormat(CultureInfo.InvariantCulture,"{{\"prefab\":\"{0}\",\"model\":\"{1}\",\"triangles\":{2},\"parts\":{3},\"clip\":\"{4}\",\"clipSeconds\":{5:0.00},\"fit\":{6:0.000},\"collider\":\"{7}\"}}",
            root.name,modelName,tris,renderers.Length,clip!=null ? clip.name : "",clip!=null ? clip.length : 0,fit,size);
    }

    static Vector2 ColliderSize(GameObject go)
    {
        var box=go.GetComponent<BoxCollider2D>();if(box!=null)return box.size;
        var circle=go.GetComponent<CircleCollider2D>();if(circle!=null)return Vector2.one*circle.radius*2;
        var polygon=go.GetComponent<PolygonCollider2D>();
        if(polygon!=null)
        {
            var min=Vector2.positiveInfinity;var max=Vector2.negativeInfinity;
            for(int path=0;path<polygon.pathCount;path++)foreach(var p in polygon.GetPath(path)){min=Vector2.Min(min,p);max=Vector2.Max(max,p);}
            return max-min;
        }
        var capsule=go.GetComponent<CapsuleCollider2D>();if(capsule!=null)return capsule.size;
        return Vector2.one;
    }

    static void Edit(string path,Action<GameObject> edit)
    {var go=PrefabUtility.LoadPrefabContents(path);try{edit(go);PrefabUtility.SaveAsPrefabAsset(go,path);}finally{PrefabUtility.UnloadPrefabContents(go);}}

    // ------------------------------------------------------------------ capture
    static void Capture(int stage)
    {
        if(stage==1)
        {
            if(!EditorApplication.isPlaying)return;
            double now=EditorApplication.timeSinceStartup;
            float start=SessionState.GetFloat(StartKey,0);
            if(start<=0){SessionState.SetFloat(StartKey,(float)now);return;}
            int shot=SessionState.GetInt("PSXv2CaptureShot",0);
            if(now-start<6+shot*.35f)return;
            var presentation=UnityEngine.Camera.main!=null ? UnityEngine.Camera.main.GetComponent<PSXPresentation>() : null;
            if(presentation!=null && presentation.SceneTexture!=null)
            {
                var scene=presentation.SceneTexture;
                var target=RenderTexture.GetTemporary(scene.width,scene.height,0,RenderTextureFormat.ARGB32);
                if(presentation.filterEnabled && presentation.CompositeMaterial!=null)Graphics.Blit(scene,target,presentation.CompositeMaterial);else Graphics.Blit(scene,target);
                var active=RenderTexture.active;RenderTexture.active=target;
                var image=new Texture2D(scene.width,scene.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,scene.width,scene.height),0,0);image.Apply();
                RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);
                File.WriteAllBytes(CaptureFolder+"/frame_"+shot+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
                if(shot==0)File.WriteAllText(CaptureFolder+"/capture.json","{\"width\":"+scene.width+",\"height\":"+scene.height+",\"filterEnabled\":"+presentation.filterEnabled.ToString().ToLowerInvariant()+
                    ",\"presentations\":"+UnityEngine.Object.FindObjectsByType<PSXPresentation>(FindObjectsSortMode.None).Length+",\"renderHeight\":"+presentation.renderHeight+",\"explosions\":"+PSXImpactEffects.ExplosionCount+",\"bursts\":"+PSXImpactEffects.BurstCount+"}");
            }
            shot++;SessionState.SetInt("PSXv2CaptureShot",shot);
            if(shot>=4){SessionState.SetInt("PSXv2CaptureShot",0);SessionState.SetInt(StageKey,2);EditorApplication.isPlaying=false;}
            return;
        }
        if(stage==2 && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SessionState.SetInt(StageKey,0);
            EditorSceneManager.OpenScene("Assets/Scenes/L1.unity");
            File.WriteAllText(CaptureFolder+"/done.txt",DateTime.Now.ToString("s"));
        }
    }
}
